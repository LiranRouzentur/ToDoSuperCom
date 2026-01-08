#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Start TaskSystem with native watchers (dotnet watch + vite)
.DESCRIPTION
    Uses bind mounts and framework-native watchers for instant feedback.
    Checks for prerequisites and provides installation guidance.
    New structure: /src/backend, /frontend/app, /infra/docker-compose.yml
.PARAMETER Clean
    Remove all containers, volumes, and orphans before starting
#>

param(
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ComposeFile = Join-Path (Join-Path $RootDir "infra") "docker-compose.yml"

Write-Host "`n=== TaskSystem - Dev Environment ===`n" -ForegroundColor Cyan

# ============================================
# PREREQUISITE CHECKS
# ============================================

$allPrereqsMet = $true

# Check 1: Docker Desktop
Write-Host "[CHECK] Verifying Docker Desktop..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[✓] Docker installed: $dockerVersion" -ForegroundColor Green
        
        # Check if Docker daemon is running
        docker ps >$null 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[✓] Docker daemon is running" -ForegroundColor Green
        } else {
            Write-Host "[✗] Docker is installed but not running" -ForegroundColor Red
            Write-Host "[ACTION] Please start Docker Desktop and try again`n" -ForegroundColor Yellow
            $allPrereqsMet = $false
        }
    } else {
        throw "Docker not found"
    }
} catch {
    Write-Host "[✗] Docker Desktop is not installed" -ForegroundColor Red
    Write-Host "[ACTION] Install Docker Desktop:" -ForegroundColor Yellow
    Write-Host "  1. Visit: https://www.docker.com/products/docker-desktop" -ForegroundColor Cyan
    Write-Host "  2. Download and install Docker Desktop for Windows" -ForegroundColor Cyan
    Write-Host "  3. Start Docker Desktop" -ForegroundColor Cyan
    Write-Host "  4. Run this script again`n" -ForegroundColor Cyan
    $allPrereqsMet = $false
}

# Check 2: Docker Compose (usually bundled with Docker Desktop)
Write-Host "[CHECK] Verifying Docker Compose..." -ForegroundColor Yellow
try {
    $composeVersion = docker compose version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[✓] Docker Compose available: $composeVersion" -ForegroundColor Green
    } else {
        throw "Docker Compose not found"
    }
} catch {
    Write-Host "[✗] Docker Compose not available" -ForegroundColor Red
    Write-Host "[INFO] Docker Compose is usually bundled with Docker Desktop" -ForegroundColor Yellow
    Write-Host "[ACTION] Ensure you have the latest Docker Desktop installed`n" -ForegroundColor Yellow
    $allPrereqsMet = $false
}

# Check 3: docker-compose.yml exists
Write-Host "[CHECK] Verifying docker-compose.yml..." -ForegroundColor Yellow
if (Test-Path $ComposeFile) {
    Write-Host "[✓] docker-compose.yml found" -ForegroundColor Green
} else {
    Write-Host "[✗] docker-compose.yml not found at: $ComposeFile" -ForegroundColor Red
    Write-Host "[ACTION] Ensure you're running this script from the correct directory`n" -ForegroundColor Yellow
    $allPrereqsMet = $false
}

# Optional Checks (for development)
Write-Host "`n[OPTIONAL] Development Tools (not required for Docker-only setup):" -ForegroundColor Cyan

# Check .NET SDK (optional - only needed for local development)
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[✓] .NET SDK installed: $dotnetVersion" -ForegroundColor Green
    }
} catch {
    Write-Host "[i] .NET SDK not installed (optional - only needed for local dev)" -ForegroundColor Gray
}

# Check Node.js (optional - only needed for local development)
try {
    $nodeVersion = node --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[✓] Node.js installed: $nodeVersion" -ForegroundColor Green
    }
} catch {
    Write-Host "[i] Node.js not installed (optional - only needed for local dev)" -ForegroundColor Gray
}

Write-Host ""

# Exit if prerequisites not met
if (-not $allPrereqsMet) {
    Write-Host "[ERROR] Prerequisites not met. Please install missing components and try again.`n" -ForegroundColor Red
    exit 1
}

# ============================================
# START DOCKER SERVICES
# ============================================

Write-Host "Native Watchers: dotnet watch + Vite HMR" -ForegroundColor Green
Write-Host "Structure: /src/backend + /frontend/app + /infra`n" -ForegroundColor Green

if ($Clean) {
    Write-Host "[CLEAN] Cleaning up old containers and volumes..." -ForegroundColor Yellow
    Push-Location $RootDir
    docker compose -f $ComposeFile down -v --remove-orphans
    $cleanResult = $LASTEXITCODE
    Pop-Location
    
    if ($cleanResult -ne 0) {
        Write-Host "`n[ERROR] Cleanup failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[✓] Cleanup complete`n" -ForegroundColor Green
}

Write-Host "[START] Starting services with docker compose..." -ForegroundColor Yellow
Write-Host "This may take a few minutes on first run (downloading images)...`n" -ForegroundColor Cyan

Push-Location $RootDir
docker compose -f $ComposeFile up --build -d
$startResult = $LASTEXITCODE
Pop-Location

if ($startResult -ne 0) {
    Write-Host "`n[ERROR] Failed to start services" -ForegroundColor Red
    Write-Host "[TIP] Check Docker Desktop is running and has enough resources`n" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n[SUCCESS] Services started successfully!`n" -ForegroundColor Green

# ============================================
# SERVICE STATUS & ACCESS INFO
# ============================================

Write-Host "=== Service URLs ===" -ForegroundColor Cyan
Write-Host "Frontend:           http://localhost:5173" -ForegroundColor White
Write-Host "API:                http://localhost:5000" -ForegroundColor White
Write-Host "API Health:         http://localhost:5000/health" -ForegroundColor White
Write-Host "RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White

Write-Host "`n=== Useful Commands ===" -ForegroundColor Cyan
Write-Host "View logs:          docker compose -f infra/docker-compose.yml logs -f" -ForegroundColor Gray
Write-Host "Stop services:      docker compose -f infra/docker-compose.yml down" -ForegroundColor Gray
Write-Host "Restart services:   docker compose -f infra/docker-compose.yml restart" -ForegroundColor Gray
Write-Host "Clean restart:      .\scripts\start-docker.ps1 -Clean`n" -ForegroundColor Gray

Write-Host "[INFO] Services are starting up. Waiting for initialization...`n" -ForegroundColor Yellow

# Wait for services to initialize
Write-Host "[WAIT] Waiting 15 seconds for services to fully initialize..." -ForegroundColor Cyan
Start-Sleep -Seconds 15

Write-Host "`n[LOGS] Following service logs (Press Ctrl+C to stop)...`n" -ForegroundColor Green
Write-Host "=" * 80 -ForegroundColor DarkGray

# Follow logs - this will keep the script alive
Push-Location $RootDir
docker compose -f $ComposeFile logs -f
Pop-Location
