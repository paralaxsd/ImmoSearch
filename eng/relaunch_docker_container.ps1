param(
    [switch]$Build,
    [string]$AdminToken,
    [string]$DataDir
)

$ErrorActionPreference = 'Stop'

try {
    Push-Location $PSScriptRoot
    $composeFile = Join-Path $PSScriptRoot 'docker/docker-compose.yml'
    if (-not (Test-Path $composeFile)) {
        throw "docker-compose.yml not found at $composeFile"
    }

    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "Docker CLI not found in PATH. Please install Docker Desktop or CLI."
    }

    try {
        docker info | Out-Null
    }
    catch {
        throw "Docker daemon not reachable. Ensure Docker Desktop is running."
    }

    if ($AdminToken) { $env:ADMIN_TOKEN = $AdminToken }

    if (-not $DataDir) { $DataDir = Join-Path $PSScriptRoot '..\assets\docker' }
    $resolvedData = Resolve-Path -Path $DataDir -ErrorAction SilentlyContinue
    if (-not $resolvedData) {
        New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
        $resolvedData = Resolve-Path -Path $DataDir -ErrorAction Stop
    }
    $env:DATA_DIR = $resolvedData.ProviderPath
    Write-Host "Resolved data dir to $env:DATA_DIR." -ForegroundColor Cyan

    if ($Build.IsPresent) {
        Write-Host "Building containers via docker compose..." -ForegroundColor Cyan
        docker compose -f $composeFile build
        if ($LASTEXITCODE -ne 0) { throw "docker compose build failed." }
    }

    Write-Host "Stopping existing containers (keeps volumes)..." -ForegroundColor Cyan
    docker compose -f $composeFile down
    if ($LASTEXITCODE -ne 0) { throw "docker compose down failed." }

    Write-Host "Starting containers..." -ForegroundColor Cyan
    docker compose -f $composeFile up -d
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed." }
    Write-Host "Containers relaunched." -ForegroundColor Green
}
catch {
    Write-Error $_
    exit 1
}
finally {
    Pop-Location
}
