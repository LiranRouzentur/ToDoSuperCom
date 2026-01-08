#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run backend or frontend tests
.DESCRIPTION
    Unified test runner with flags to run either backend or frontend tests.
    Cannot run both simultaneously.
    
    IMPORTANT:
    - Backend tests: Unit tests run without Docker. Integration tests may need Docker.
    - Frontend tests: REQUIRE Docker to be running (API + services must be available)
    
.PARAMETER Backend
    Run backend tests (dotnet test)
.PARAMETER Frontend
    Run frontend e2e tests (Playwright) - REQUIRES Docker running
.PARAMETER SkipDockerCheck
    Skip Docker availability check (use with caution)
.EXAMPLE
    .\run-tests.ps1 -Backend
    .\run-tests.ps1 -Frontend
#>

param(
    [switch]$Backend,
    [switch]$Frontend,
    [switch]$SkipDockerCheck
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir

# Validate: Only one flag at a time
if ($Backend -and $Frontend) {
    Write-Host "`n[ERROR] Cannot run both Backend and Frontend tests simultaneously." -ForegroundColor Red
    Write-Host "Please specify only one flag: -Backend OR -Frontend`n" -ForegroundColor Yellow
    exit 1
}

if (-not $Backend -and -not $Frontend) {
    Write-Host "`n[ERROR] No test target specified." -ForegroundColor Red
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\run-tests.ps1 -Backend    # Run backend tests" -ForegroundColor Cyan
    Write-Host "  .\run-tests.ps1 -Frontend   # Run frontend e2e tests (requires Docker)`n" -ForegroundColor Cyan
    exit 1
}

# Function to check if Docker is running
function Test-DockerRunning {
    try {
        $null = docker ps 2>&1
        return $LASTEXITCODE -eq 0
    } catch {
        return $false
    }
}

# Function to check if API is responding
function Test-ApiAvailable {
    param([string]$Url = "http://localhost:5000/health")
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -UseBasicParsing
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

# Run Backend Tests
if ($Backend) {
    Write-Host "`n=== Running Backend Tests ===`n" -ForegroundColor Cyan
    
    $SolutionPath = Join-Path (Join-Path $RootDir "src") "backend\TaskSystem.sln"
    
    Write-Host "[BUILD] Building solution..." -ForegroundColor Yellow
    dotnet build $SolutionPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[ERROR] Build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n[TEST] Running all backend tests...`n" -ForegroundColor Yellow
    Write-Host "[INFO] Unit tests will run without Docker." -ForegroundColor Cyan
    Write-Host "[INFO] Integration tests may require Docker to be running.`n" -ForegroundColor Cyan
    
    dotnet test $SolutionPath --no-build --logger "console;verbosity=normal"
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[ERROR] Backend tests failed" -ForegroundColor Red
        Write-Host "[TIP] If integration tests failed, ensure Docker is running: .\start-docker.ps1`n" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "`n[SUCCESS] All backend tests passed!`n" -ForegroundColor Green
}

# Run Frontend Tests
if ($Frontend) {
    Write-Host "`n=== Running Frontend E2E Tests ===`n" -ForegroundColor Cyan
    
    # Check Docker is running (unless skipped)
    if (-not $SkipDockerCheck) {
        Write-Host "[CHECK] Verifying Docker is running..." -ForegroundColor Yellow
        
        if (-not (Test-DockerRunning)) {
            Write-Host "`n[ERROR] Docker is not running!" -ForegroundColor Red
            Write-Host "[ACTION] Please start Docker first:" -ForegroundColor Yellow
            Write-Host "  cd scripts" -ForegroundColor Cyan
            Write-Host "  .\start-docker.ps1`n" -ForegroundColor Cyan
            exit 1
        }
        
        Write-Host "[✓] Docker is running" -ForegroundColor Green
        
        # Check if API is available
        Write-Host "[CHECK] Verifying API is available..." -ForegroundColor Yellow
        
        $maxRetries = 3
        $retryCount = 0
        $apiAvailable = $false
        
        while ($retryCount -lt $maxRetries -and -not $apiAvailable) {
            $apiAvailable = Test-ApiAvailable
            if (-not $apiAvailable) {
                $retryCount++
                if ($retryCount -lt $maxRetries) {
                    Write-Host "[WAIT] API not ready, retrying ($retryCount/$maxRetries)..." -ForegroundColor Yellow
                    Start-Sleep -Seconds 2
                }
            }
        }
        
        if (-not $apiAvailable) {
            Write-Host "`n[WARNING] API health check failed!" -ForegroundColor Yellow
            Write-Host "[INFO] Containers may still be starting up." -ForegroundColor Cyan
            Write-Host "[TIP] If tests fail, wait a moment and try again, or run: .\start-docker.ps1`n" -ForegroundColor Cyan
        } else {
            Write-Host "[✓] API is responding" -ForegroundColor Green
        }
    }
    
    $FrontendPath = Join-Path (Join-Path $RootDir "frontend") "app"
    
    # Check if node_modules exists
    if (-not (Test-Path (Join-Path $FrontendPath "node_modules"))) {
        Write-Host "`n[SETUP] Installing dependencies..." -ForegroundColor Yellow
        Push-Location $FrontendPath
        npm install
        Pop-Location
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "`n[ERROR] npm install failed" -ForegroundColor Red
            exit 1
        }
    }
    
    # Check if Playwright browsers are installed
    Write-Host "`n[CHECK] Verifying Playwright installation..." -ForegroundColor Yellow
    Push-Location $FrontendPath
    
    # Try to install Playwright browsers if needed
    npx playwright install --with-deps 2>$null
    
    Write-Host "`n[TEST] Running Playwright tests...`n" -ForegroundColor Yellow
    npm run test:e2e
    
    $testResult = $LASTEXITCODE
    Pop-Location
    
    if ($testResult -ne 0) {
        Write-Host "`n[ERROR] Frontend tests failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n[SUCCESS] All frontend tests passed!`n" -ForegroundColor Green
}
