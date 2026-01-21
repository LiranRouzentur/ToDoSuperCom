#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run backend or frontend tests
.DESCRIPTION
    Unified test runner.
.PARAMETER Backend
    Run backend tests
.PARAMETER Frontend
    Run frontend e2e tests
.PARAMETER SkipDockerCheck
    Skip Docker availability check
#>

param(
    [switch]$Backend,
    [switch]$Frontend,
    [switch]$SkipDockerCheck
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$ArtifactsDir = Join-Path $RootDir "artifacts"
if (-not (Test-Path $ArtifactsDir)) { New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null }

# Test Results Collection
$TestResults = [System.Collections.Generic.List[PSObject]]::new()

function Add-TestResult {
    param(
        [string]$Name,
        [string]$Status, # "Success" or "Failed"
        [string]$Category, # "Backend" or "Frontend" or "Infra"
        [string]$ErrorMessage = $null
    )
    
    $script:TestResults.Add([PSCustomObject]@{
        Name = $Name
        Status = $Status
        Category = $Category
        ErrorMessage = $ErrorMessage
    })
}

function Format-TestName {
    param([string]$RawName)

    # 1. Extract the last part (Method name) if it looks like a namespace
    if ($RawName -match '([^\.]+)$') {
        $RawName = $matches[1]
    }
    
    # 2. Replace underscore with space
    $Friendly = $RawName -replace "_", " "
    
    return $Friendly
}

# Validate: Only one flag at a time
if ($Backend -and $Frontend) {
    Write-Host "[ERROR] Cannot run both Backend and Frontend tests simultaneously." -ForegroundColor Red
    exit 1
}

if (-not $Backend -and -not $Frontend) {
    Write-Host "[ERROR] No test target specified." -ForegroundColor Red
    exit 1
}

function Test-DockerRunning {
    try {
        $null = docker ps 2>&1
        if ($LASTEXITCODE -eq 0) {
            Add-TestResult -Name "Is Docker live" -Status "Success" -Category "Infra"
            return $true
        } else {
            Add-TestResult -Name "Is Docker live" -Status "Failed" -Category "Infra"
            return $false
        }
    } catch {
        Add-TestResult -Name "Is Docker live" -Status "Failed" -Category "Infra"
        return $false
    }
}

function Test-ApiAvailable {
    param([string]$Url = "http://localhost:5000/health")
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 5 -UseBasicParsing
        return $response.StatusCode -eq 200
    } catch {
        return $false
    }
}

try {
    # Run Backend Tests
    if ($Backend) {
        Write-Host "=== Running Backend Tests ===" -ForegroundColor Cyan
        
        $SolutionPath = Join-Path (Join-Path $RootDir "src") "backend\TaskSystem.sln"
        
        # Clean previous TRX files to ensure we only parse current run results
        Get-ChildItem -Path $ArtifactsDir -Filter "*.trx" | Remove-Item -Force
        
        Write-Host "[BUILD] Building solution..." -ForegroundColor Yellow
        dotnet build $SolutionPath
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] Build failed" -ForegroundColor Red
            Add-TestResult -Name "Build Backend Solution" -Status "Failed" -Category "Backend"
            exit 1
        } else {
            Add-TestResult -Name "Build Backend Solution" -Status "Success" -Category "Backend"
        }
        
        Write-Host "[TEST] Running all backend tests..." -ForegroundColor Yellow
        
        # Run tests with TRX logger, logging to the directory. dotnet will create unique filenames for each project.
        dotnet test $SolutionPath --no-build --logger "trx" --results-directory "$ArtifactsDir" --logger "console;verbosity=normal"
        
        $executionResult = $LASTEXITCODE
        
        # Parse ALL TRX files in the directory
        $trxFiles = Get-ChildItem -Path $ArtifactsDir -Filter "*.trx"
        
        if ($trxFiles) {
            foreach ($trxFile in $trxFiles) {
                # Use -LiteralPath to avoid issues with brackets in filenames (e.g. test_results[1].trx)
                [xml]$trx = Get-Content -LiteralPath $trxFile.FullName -Raw
                $ns = @{ns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
                
                # Select all UnitTestResults
                $results = Select-Xml -Xml $trx -XPath "//ns:UnitTestResult" -Namespace $ns
                
                foreach ($res in $results) {
                    $testName = $res.Node.testName
                    $outcome = $res.Node.outcome
                    $status = if ($outcome -eq "Passed") { "Success" } else { "Failed" }
                    
                    $errorMsg = $null
                    if ($status -eq "Failed") {
                        # Try to extract error message
                        $errorMsg = $res.Node.Output.ErrorInfo.Message
                    }
                    
                    Add-TestResult -Name $testName -Status $status -Category "Backend" -ErrorMessage $errorMsg
                }
            }
        }
        
        if ($executionResult -ne 0) {
            Write-Host "[ERROR] Backend tests failed" -ForegroundColor Red
        } else {
            Write-Host "[SUCCESS] All backend tests passed!" -ForegroundColor Green
        }
    }

    # Run Frontend Tests
    if ($Frontend) {
        Write-Host "=== Running Frontend E2E Tests ===" -ForegroundColor Cyan
        
        if (-not $SkipDockerCheck) {
            Write-Host "[CHECK] Verifying Docker is running..." -ForegroundColor Yellow
            
            if (-not (Test-DockerRunning)) {
                Write-Host "[ERROR] Docker is not running!" -ForegroundColor Red
                exit 1
            }
            
            Write-Host "[OK] Docker is running" -ForegroundColor Green
            
            Write-Host "[CHECK] Verifying API is available..." -ForegroundColor Yellow
            
            $maxRetries = 3
            $retryCount = 0
            $apiAvailable = $false
            
            while ($retryCount -lt $maxRetries -and -not $apiAvailable) {
                $apiAvailable = Test-ApiAvailable
                if (-not $apiAvailable) {
                    $retryCount++
                    if ($retryCount -lt $maxRetries) {
                        Write-Host "[WAIT] Retrying API check..." -ForegroundColor Yellow
                        Start-Sleep -Seconds 2
                    }
                }
            }
            
            if (-not $apiAvailable) {
                Write-Host "[WARNING] API health check failed!" -ForegroundColor Yellow
                Add-TestResult -Name "API Health Check" -Status "Failed" -Category "Infra"
            } else {
                Write-Host "[OK] API is responding" -ForegroundColor Green
                Add-TestResult -Name "API Health Check" -Status "Success" -Category "Infra"
            }
        }
        
        $FrontendPath = Join-Path (Join-Path $RootDir "frontend") "app"
        $JsonPath = Join-Path $ArtifactsDir "frontend_tests.json"
        
        if (-not (Test-Path (Join-Path $FrontendPath "node_modules"))) {
            Write-Host "[SETUP] Installing dependencies..." -ForegroundColor Yellow
            Push-Location $FrontendPath
            npm install
            Pop-Location
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "[ERROR] npm install failed" -ForegroundColor Red
                Add-TestResult -Name "Install Frontend Dependencies" -Status "Failed" -Category "Frontend" -ErrorMessage "npm install returned exit code $LASTEXITCODE"
                exit 1
            } else {
                Add-TestResult -Name "Install Frontend Dependencies" -Status "Success" -Category "Frontend"
            }
        }
        
        Write-Host "[CHECK] Verifying Playwright installation..." -ForegroundColor Yellow
        Push-Location $FrontendPath
        
        npx playwright install --with-deps
        
        Write-Host "[TEST] Running Playwright tests..." -ForegroundColor Yellow
        
        $env:PLAYWRIGHT_JSON_OUTPUT_NAME = $JsonPath
        
        # We use 'list' for console output and 'json' for file output.
        $procs = Start-Process -FilePath "cmd" -ArgumentList "/c npx playwright test --reporter=list,json" -Wait -NoNewWindow -PassThru
        
        $testResult = $procs.ExitCode
        Pop-Location
        
        # Parse JSON results
        if (Test-Path $JsonPath) {
            try {
                $jsonContent = Get-Content $JsonPath -Raw | ConvertFrom-Json
                foreach ($suite in $jsonContent.suites) {
                    foreach ($spec in $suite.specs) {
                        $testName = $spec.title
                        $status = "Failed"
                        $errorMsg = $null
                        
                        if ($spec.ok) { 
                            $status = "Success" 
                        } else {
                             # Try to capture first error
                             if ($spec.tests[0].results[0].error.message) {
                                $errorMsg = $spec.tests[0].results[0].error.message
                             }
                        }
                        
                        Add-TestResult -Name $testName -Status $status -Category "Frontend" -ErrorMessage $errorMsg
                    }
                }
            } catch {
                Write-Host "[WARNING] Failed to parse frontend test results: $_" -ForegroundColor Yellow
            }
        }

        if ($testResult -ne 0) {
            Write-Host "[ERROR] Frontend tests failed" -ForegroundColor Red
        } else {
            Write-Host "[SUCCESS] All frontend tests passed!" -ForegroundColor Green
        }
    }
}
finally {
    Write-Host "`n============================================================" -ForegroundColor Magenta
    Write-Host "                   TEST RUN SUMMARY" -ForegroundColor Magenta
    Write-Host "============================================================" -ForegroundColor Magenta
    
    $totalPassed = 0
    $totalFailed = 0
    
    foreach ($result in $TestResults) {
        $color = if ($result.Status -eq "Success") { 
            $totalPassed++
            "Green" 
        } else { 
            $totalFailed++
            "Red" 
        }
        
        $friendlyName = Format-TestName -RawName $result.Name
        
        Write-Host "Test: $friendlyName - $($result.Status)" -ForegroundColor $color
        
        if ($result.Status -eq "Failed" -and $result.ErrorMessage) {
            Write-Host "      Error: $($result.ErrorMessage)" -ForegroundColor DarkRed
        }
    }
    
    Write-Host "------------------------------------------------------------" -ForegroundColor Magenta
    Write-Host "Total: $($TestResults.Count) | Passed: $totalPassed | Failed: $totalFailed" -ForegroundColor Magenta
    Write-Host "============================================================" -ForegroundColor Magenta
}

