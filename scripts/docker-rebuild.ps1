<#
.SYNOPSIS
    Rebuilds and restarts IMS services in Docker Compose.

.DESCRIPTION
    A compact replacement for "docker compose up -d --build" so you don't
    have to remember it every time. With no parameter, rebuilds everything.

.PARAMETER Service
    Name of a specific service from docker-compose.yml (e.g. api-gateway,
    accounts-api, inventory-api). If omitted, all services are rebuilt.

.EXAMPLE
    ./scripts/docker-rebuild.ps1
    Rebuilds and brings up the whole stack.

.EXAMPLE
    ./scripts/docker-rebuild.ps1 api-gateway
    Rebuilds and restarts only api-gateway.
#>
param(
    [string]$Service
)

$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

if ($Service) {
    docker compose up -d --build $Service
} else {
    docker compose up -d --build
}

docker compose ps
