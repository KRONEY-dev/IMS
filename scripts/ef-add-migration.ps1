<#
.SYNOPSIS
    Generates a new EF Core migration for one of the IMS microservices.

.DESCRIPTION
    A compact replacement for manually typing "dotnet ef migrations add" with the
    correct --project/--startup-project/--output-dir paths, which are easy to mix up.
    The dotnet-ef version comes from .config/dotnet-tools.json (local tool).
    Run with no parameters for an interactive prompt, or pass -Service/-Name directly.

.PARAMETER Service
    "accounts" or "inventory" - which microservice. Prompted interactively if omitted.

.PARAMETER Name
    Migration name (e.g. AddSomething). Prompted interactively if omitted.

.EXAMPLE
    ./scripts/ef-add-migration.ps1
    Prompts for the service and migration name interactively.

.EXAMPLE
    ./scripts/ef-add-migration.ps1 accounts AddSomething
    Runs directly with no prompts.
#>
param(
    [ValidateSet("accounts", "inventory")]
    [string]$Service,

    [string]$Name
)

$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot

if (-not $Service) {
    Write-Host "Select a service:"
    Write-Host "  1) accounts"
    Write-Host "  2) inventory"
    $choice = Read-Host "Enter 1 or 2"

    $Service = switch ($choice) {
        "1" { "accounts" }
        "2" { "inventory" }
        default { throw "Invalid choice '$choice' - expected 1 or 2." }
    }
}

if (-not $Name) {
    $Name = Read-Host "Migration name"
}

if (-not $Name) {
    throw "Migration name cannot be empty."
}

$projectMap = @{
    accounts  = @{
        Project        = "AccountsService/AccountsService.Infrastructure/AccountsService.Infrastructure.csproj"
        StartupProject = "AccountsService/AccountsService.API/AccountsService.API.csproj"
    }
    inventory = @{
        Project        = "InventoryService/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj"
        StartupProject = "InventoryService/InventoryService.API/InventoryService.API.csproj"
    }
}

$config = $projectMap[$Service]

dotnet tool restore

dotnet ef migrations has-pending-model-changes `
    --project $config.Project `
    --startup-project $config.StartupProject

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "No model changes detected since the last migration - skipping, a new migration would be empty." -ForegroundColor Yellow
    exit 0
}

dotnet ef migrations add $Name `
    --project $config.Project `
    --startup-project $config.StartupProject `
    --output-dir Database/Migrations

if ($?) {
    Write-Host ""
    Write-Host "Migration generated. Before committing, open the new file under Database/Migrations and review Up()/Down() by hand - EF generation is not guaranteed to be correct (e.g. a rename can come out as drop+add)." -ForegroundColor Yellow
}
