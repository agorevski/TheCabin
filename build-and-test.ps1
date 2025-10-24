# The Cabin - Build and Test Script
# This script builds the project, runs all tests, and provides a summary

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  The Cabin - Build and Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ErrorActionPreference = "Continue"
$buildSuccess = $true
$testSuccess = $true

# Step 1: Clean previous builds
Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Clean failed" -ForegroundColor Red
    $buildSuccess = $false
} else {
    Write-Host "√ Clean successful" -ForegroundColor Green
}
Write-Host ""

# Step 2: Restore NuGet packages
Write-Host "[2/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Restore failed" -ForegroundColor Red
    $buildSuccess = $false
} else {
    Write-Host "√ Restore successful" -ForegroundColor Green
}
Write-Host ""

# Step 3: Build Core library
Write-Host "[3/5] Building TheCabin.Core..." -ForegroundColor Yellow
dotnet build src/TheCabin.Core/TheCabin.Core.csproj --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Core build failed" -ForegroundColor Red
    $buildSuccess = $false
} else {
    Write-Host "√ Core build successful" -ForegroundColor Green
}
Write-Host ""

# Step 4: Build Infrastructure library
Write-Host "[4/5] Building TheCabin.Infrastructure..." -ForegroundColor Yellow
dotnet build src/TheCabin.Infrastructure/TheCabin.Infrastructure.csproj --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Infrastructure build failed" -ForegroundColor Red
    $buildSuccess = $false
} else {
    Write-Host "√ Infrastructure build successful" -ForegroundColor Green
}
Write-Host ""

# Step 5: Run Tests
Write-Host "[5/5] Running all tests..." -ForegroundColor Yellow
dotnet test tests/TheCabin.Core.Tests/TheCabin.Core.Tests.csproj --configuration Release --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Tests failed or had errors" -ForegroundColor Red
    $testSuccess = $false
} else {
    Write-Host "√ Tests completed" -ForegroundColor Green
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Build and Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($buildSuccess) {
    Write-Host "√ Build: SUCCESS" -ForegroundColor Green
} else {
    Write-Host "X Build: FAILED" -ForegroundColor Red
}

if ($testSuccess) {
    Write-Host "√ Tests: PASSED" -ForegroundColor Green
} else {
    Write-Host "X Tests: FAILED (check details above)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Done!" -ForegroundColor Cyan
