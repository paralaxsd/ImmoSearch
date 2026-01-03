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

    Write-Host "Building containers via docker compose..." -ForegroundColor Cyan
    docker compose -f $composeFile build
    Write-Host "Build completed." -ForegroundColor Green
}
catch {
    Write-Error $_
    exit 1
}
finally {
    Pop-Location
}
