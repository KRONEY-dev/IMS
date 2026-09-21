<#
.SYNOPSIS
    Перебілдовує та перезапускає сервіси IMS у Docker Compose.

.DESCRIPTION
    Компактна заміна для "docker compose up -d --build", яку не треба
    згадувати щоразу вручну. Без параметра ребілдить усе.

.PARAMETER Service
    Ім'я конкретного сервісу з docker-compose.yml (напр. api-gateway,
    accounts-api, inventory-api). Якщо не вказано — ребілдяться всі сервіси.

.EXAMPLE
    ./scripts/docker-rebuild.ps1
    Перебілдовує й піднімає весь стек.

.EXAMPLE
    ./scripts/docker-rebuild.ps1 api-gateway
    Перебілдовує й перезапускає тільки api-gateway.
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
