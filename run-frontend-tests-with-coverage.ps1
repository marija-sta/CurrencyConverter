#!/usr/bin/env pwsh

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Frontend Test Coverage Report" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

$clientPath = Join-Path $PSScriptRoot "client"
$testResultsPath = Join-Path $PSScriptRoot "TestResults"
$frontendCoveragePath = Join-Path $testResultsPath "frontend-coverage"

# Ensure TestResults directory exists
if (-not (Test-Path $testResultsPath)) {
    New-Item -ItemType Directory -Path $testResultsPath -Force | Out-Null
}

Write-Host "Running frontend tests with coverage..." -ForegroundColor Yellow
Write-Host ""

# Navigate to client directory and run tests with coverage
Push-Location $clientPath
try {
    # Run tests with coverage
    npm run test:coverage
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host "Tests passed successfully!" -ForegroundColor Green
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host ""
        
        # Check if coverage reports were generated
        if (Test-Path $frontendCoveragePath) {
            Write-Host "Coverage reports generated at:" -ForegroundColor Cyan
            Write-Host "  HTML Report: $frontendCoveragePath\index.html" -ForegroundColor White
            Write-Host "  Cobertura:   $frontendCoveragePath\cobertura-coverage.xml" -ForegroundColor White
            Write-Host ""
            
            # Display coverage summary from coverage-summary.json
            $summaryCoveragePath = Join-Path $frontendCoveragePath "coverage-summary.json"
            if (Test-Path $summaryCoveragePath) {
                $summary = Get-Content $summaryCoveragePath | ConvertFrom-Json
                $total = $summary.total
                
                Write-Host "Coverage Summary:" -ForegroundColor Cyan
                Write-Host "  Lines:      $($total.lines.pct)%" -ForegroundColor White
                Write-Host "  Statements: $($total.statements.pct)%" -ForegroundColor White
                Write-Host "  Functions:  $($total.functions.pct)%" -ForegroundColor White
                Write-Host "  Branches:   $($total.branches.pct)%" -ForegroundColor White
                Write-Host ""
            }
            
            # Ask if user wants to open the HTML report
            Write-Host "Open HTML coverage report? (Y/N): " -NoNewline -ForegroundColor Cyan
            $response = Read-Host
            if ($response -eq 'Y' -or $response -eq 'y') {
                $htmlReport = Join-Path $frontendCoveragePath "index.html"
                Start-Process $htmlReport
            }
        } else {
            Write-Host "Warning: Coverage reports not found at expected location" -ForegroundColor Yellow
        }
    } else {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Red
        Write-Host "Tests failed!" -ForegroundColor Red
        Write-Host "=====================================" -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
