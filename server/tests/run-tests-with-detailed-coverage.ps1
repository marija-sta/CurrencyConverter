# Run Tests with Detailed Coverage Report
# This script runs all tests and generates an HTML coverage report

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

# Generate report
$reportsParam = $coverageFiles -join ";"
reportgenerator "-reports:$reportsParam" "-targetdir:./TestResults/CoverageReport" "-reporttypes:Html;HtmlSummary;Badges;TextSummary" "-verbosity:Info"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Coverage report generation failed!" -ForegroundColor Red
    Write-Host "Make sure reportgenerator is installed: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Coverage Report Generated Successfully!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

# Display summary
if (Test-Path "./TestResults/CoverageReport/Summary.txt") {
    Write-Host "Coverage Summary:" -ForegroundColor Cyan
    Get-Content "./TestResults/CoverageReport/Summary.txt"
}

Write-Host "`nDetailed HTML Report: ./TestResults/CoverageReport/index.html" -ForegroundColor Green
Write-Host "`nOpening report in browser..." -ForegroundColor Cyan
Start-Process "./TestResults/CoverageReport/index.html"

Write-Host "`nDone!" -ForegroundColor Green
