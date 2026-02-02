# Frontend Test Coverage Script
# This script runs from CurrencyConverter/scripts/ folder
# Coverage is generated to CurrencyConverter/TestResults/frontend-coverage (configured in vitest.config.ts)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$clientDir = Join-Path $scriptDir "..\client"
$testResultsDir = Join-Path $scriptDir "..\TestResults"
$frontendCoverageDir = Join-Path $testResultsDir "frontend-coverage"
# Ensure TestResults directory exists
if (-not (Test-Path $testResultsDir)) {
    Write-Host "Creating TestResults directory..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $testResultsDir -Force | Out-Null
}
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Frontend Test Coverage Report" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
# Navigate to client directory
Push-Location $clientDir
try {
    Write-Host "Running frontend tests with coverage..." -ForegroundColor Yellow
    Write-Host ""
    # Run tests with coverage (vitest.config.ts already configured to save to ../TestResults/frontend-coverage)
    npm run test:coverage
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host "Tests passed successfully!" -ForegroundColor Green
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host ""
        # Check if coverage reports were generated
        if (Test-Path $frontendCoverageDir) {
            Write-Host "Coverage Reports Location: $frontendCoverageDir" -ForegroundColor Green
            Write-Host "  - HTML Report: $frontendCoverageDir\index.html" -ForegroundColor White
            Write-Host "  - Cobertura XML: $frontendCoverageDir\cobertura-coverage.xml" -ForegroundColor White
            Write-Host ""
            # Display coverage summary from coverage-summary.json
            $summaryCoveragePath = Join-Path $frontendCoverageDir "coverage-summary.json"
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
        } else {
            Write-Host "Warning: Coverage reports not found at expected location: $frontendCoverageDir" -ForegroundColor Yellow
        }
    } else {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Red
        Write-Host "Tests failed!" -ForegroundColor Red
        Write-Host "=====================================" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
    Write-Host "Done!" -ForegroundColor Green
} finally {
    Pop-Location
}
