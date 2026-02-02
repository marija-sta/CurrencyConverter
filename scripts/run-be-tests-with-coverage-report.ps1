# Backend Test Coverage Script
# This script runs from CurrencyConverter/scripts/ folder
# and generates coverage reports in CurrencyConverter/TestResults/
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverDir = Join-Path $scriptDir "..\server"
$testResultsDir = Join-Path $scriptDir "..\TestResults"
$backendCoverageDir = Join-Path $testResultsDir "backend-coverage"
# Ensure TestResults directory exists
if (-not (Test-Path $testResultsDir)) {
    Write-Host "Creating TestResults directory..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $testResultsDir -Force | Out-Null
}
# Navigate to server directory
Push-Location $serverDir
try {
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "Backend Test Coverage Report" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build --configuration Release --no-incremental
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "`nCleaning old test results..." -ForegroundColor Cyan
    if (Test-Path "./TestResults") {
        Remove-Item -Path "./TestResults" -Recurse -Force
    }
    Write-Host "`nRunning all tests with coverage..." -ForegroundColor Cyan
    dotnet test --configuration Release --collect:"XPlat Code Coverage" --results-directory:"./TestResults" --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "`nGenerating coverage report..." -ForegroundColor Cyan
    # Find all coverage files
    $coverageFiles = Get-ChildItem -Path "./TestResults" -Recurse -Filter "coverage.cobertura.xml" | Select-Object -ExpandProperty FullName
    if ($coverageFiles.Count -eq 0) {
        Write-Host "No coverage files found!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Gray
    # Generate HTML report directly to the central TestResults/backend-coverage folder
    $reportsParam = $coverageFiles -join ";"
    reportgenerator "-reports:$reportsParam" "-targetdir:$backendCoverageDir" "-reporttypes:Html;HtmlSummary;Badges;TextSummary;Cobertura" "-verbosity:Info"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Coverage report generation failed!" -ForegroundColor Red
        Write-Host "Make sure reportgenerator is installed: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Coverage Report Generated Successfully!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Cyan
    # Display summary
    if (Test-Path "$backendCoverageDir\Summary.txt") {
        Write-Host "Coverage Summary:" -ForegroundColor Cyan
        Get-Content "$backendCoverageDir\Summary.txt"
    }
    Write-Host "`nCoverage Reports Location: $backendCoverageDir" -ForegroundColor Green
    Write-Host "  - HTML Report: $backendCoverageDir\index.html" -ForegroundColor White
    Write-Host "  - Cobertura XML: $backendCoverageDir\Cobertura.xml" -ForegroundColor White
    Write-Host "`nOpening HTML report in browser..." -ForegroundColor Cyan
    Start-Process "$backendCoverageDir\index.html"
    Write-Host "`nDone!" -ForegroundColor Green
} finally {
    Pop-Location
}
