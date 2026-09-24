#!/usr/bin/env bash
set -euo pipefail

# Bootstraps a bare Ubuntu 24.04 VM into a running IMS deployment - every
# step that was originally done by hand on the current Oracle Cloud
# instance, verified live against it (helm list -A / helm get values /
# iptables -L) on 2026-09-24, not reconstructed from memory. Re-run this on
# a fresh host if the current server is lost or the app is migrated
# elsewhere. Idempotent where practical, so re-running on the same host to
# pick up a change is also safe.
#
# What this script deliberately does NOT do (do these yourself):
#   - Check whether the public IP is a reserved/persistent one in the OCI
#     console (Networking > IP Management) or an ephemeral one tied to this
#     VM's lifecycle - only the OCI console can tell you which, this can't
#     be determined from inside the guest OS. If ephemeral, a rebuild gets a
#     new IP and DuckDNS + the ORACLE_HOST/ORACLE_HOST_KEY secrets below all
#     need updating; if reserved, the same IP can be re-attached and none of
#     that changes.
#   - Point ims-main.duckdns.org / ims-grafana.duckdns.org at this host's IP
#     (Duck DNS web UI - there's no token configured on this box for the API).
#   - Open TCP 80/443/22 in the cloud provider's own firewall/security list
#     (separate from the OS-level iptables rules this script does add).
#   - Update the ORACLE_HOST / ORACLE_HOST_KEY GitHub Actions secrets to
#     point at this host - SSH keys and GitHub secrets are handled by the
#     project owner directly, never by an automated script.
#   - Restore application data - there is no database backup/restore
#     strategy in this project yet, so a rebuilt server starts with an
#     empty Postgres, same as a brand new deployment.
#
# Usage (as the "ubuntu" user on the target VM):
#   scp this file over, then: bash oracle-bootstrap.sh

REPO_URL="https://github.com/KRONEY-dev/IMS.git"
REPO_DIR="$HOME/IMS"

echo "==> Installing k3s"
if ! command -v k3s >/dev/null 2>&1; then
  curl -sfL https://get.k3s.io | sh -s - --disable traefik --write-kubeconfig-mode 644
else
  echo "already installed, skipping"
fi

mkdir -p "$HOME/.kube"
sudo cp /etc/rancher/k3s/k3s.yaml "$HOME/.kube/config"
sudo chown "$(id -u):$(id -g)" "$HOME/.kube/config"
export KUBECONFIG="$HOME/.kube/config"

echo "==> Waiting for the cluster to come up"
until kubectl get nodes >/dev/null 2>&1; do sleep 2; done
kubectl wait --for=condition=Ready node --all --timeout=120s

echo "==> Opening 80/443/22 in the OS firewall (separate from the cloud security list)"
sudo DEBIAN_FRONTEND=noninteractive apt-get update -y
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y iptables-persistent
for port in 80 443 22; do
  sudo iptables -C INPUT -p tcp --dport "$port" -m state --state NEW -j ACCEPT 2>/dev/null || \
    sudo iptables -I INPUT -p tcp --dport "$port" -m state --state NEW -j ACCEPT
done
sudo netfilter-persistent save

echo "==> Installing Helm"
if ! command -v helm >/dev/null 2>&1; then
  curl -fsSL https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
else
  echo "already installed, skipping"
fi

echo "==> Cloning the app repo"
if [ ! -d "$REPO_DIR/.git" ]; then
  git clone "$REPO_URL" "$REPO_DIR"
fi
cd "$REPO_DIR"

echo "==> Adding Helm repos"
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx >/dev/null
helm repo add jetstack https://charts.jetstack.io >/dev/null
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts >/dev/null
helm repo update >/dev/null

echo "==> Installing ingress-nginx (bare-metal, hostNetwork mode)"
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  --version 4.15.1 \
  --namespace ingress-nginx --create-namespace \
  --set controller.hostNetwork=true \
  --set controller.service.type=ClusterIP \
  --set controller.kind=DaemonSet \
  --set controller.dnsPolicy=ClusterFirstWithHostNet \
  --wait --timeout 5m

echo "==> Installing cert-manager"
helm upgrade --install cert-manager jetstack/cert-manager \
  --version v1.21.2 \
  --namespace cert-manager --create-namespace \
  --set crds.enabled=true \
  --wait --timeout 5m

echo "==> Applying the Let's Encrypt ClusterIssuer"
kubectl apply -f charts/cloud/letsencrypt-clusterissuer.yaml

echo "==> Installing kube-prometheus-stack (Prometheus + Grafana)"
helm upgrade --install kube-prometheus-stack prometheus-community/kube-prometheus-stack \
  --version 91.5.1 \
  --namespace monitoring --create-namespace \
  -f charts/monitoring/kube-prometheus-stack-values-oracle.yaml \
  --wait --timeout 5m

echo "==> Applying the Grafana TLS certificate"
kubectl apply -f charts/monitoring/grafana-certificate-oracle.yaml

echo "==> Generating JWT signing keys"
mkdir -p AccountsService/AccountsService.API/Keys ApiGateway/Keys
if [ ! -f AccountsService/AccountsService.API/Keys/accounts-private.pem ]; then
  openssl genrsa -out AccountsService/AccountsService.API/Keys/accounts-private.pem 2048
  openssl rsa -in AccountsService/AccountsService.API/Keys/accounts-private.pem \
    -pubout -out ApiGateway/Keys/accounts-public.pem
fi
kubectl create secret generic ims-jwt-keys \
  --from-file=accounts-private.pem=AccountsService/AccountsService.API/Keys/accounts-private.pem \
  --from-file=accounts-public.pem=ApiGateway/Keys/accounts-public.pem \
  --dry-run=client -o yaml | kubectl apply -f -

echo "==> Generating database secrets"
if [ ! -f charts/ims/values-secrets.yaml ]; then
  cat > charts/ims/values-secrets.yaml <<EOF
accountsDb:
  password: "$(openssl rand -base64 24)"
inventoryDb:
  password: "$(openssl rand -base64 24)"
EOF
  chmod 600 charts/ims/values-secrets.yaml
fi

echo "==> Deploying the IMS Helm chart"
helm upgrade --install ims charts/ims \
  -f charts/ims/values-oracle.yaml -f charts/ims/values-secrets.yaml \
  --wait --timeout 5m

cat <<'EOF'

==> Done. Remaining manual steps:
  - Point ims-main.duckdns.org / ims-grafana.duckdns.org at this host's IP (Duck DNS web UI)
  - Open TCP 80/443/22 in the cloud provider's security list/firewall, if not already
  - Update the ORACLE_HOST / ORACLE_HOST_KEY GitHub Actions secrets if the host or its IP changed
EOF
