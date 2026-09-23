{{/*
Common labels shared by every resource in this chart.
*/}}
{{- define "ims.labels" -}}
helm.sh/chart: {{ .Chart.Name }}-{{ .Chart.Version }}
app.kubernetes.io/part-of: {{ .Chart.Name }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{/*
Per-component labels. Call with (dict "context" $ "component" "<name>").
Must stay a superset of ims.selectorLabels — Kubernetes requires a
StatefulSet/Deployment selector to match a subset of its pod template labels.
*/}}
{{- define "ims.componentLabels" -}}
{{ include "ims.labels" .context }}
{{ include "ims.selectorLabels" . }}
{{- end -}}

{{/*
Per-component selector labels — must stay a stable subset of componentLabels across releases.
Call with (dict "context" $ "component" "<name>").
*/}}
{{- define "ims.selectorLabels" -}}
app.kubernetes.io/component: {{ .component }}
app.kubernetes.io/instance: {{ .context.Release.Name }}
{{- end -}}
