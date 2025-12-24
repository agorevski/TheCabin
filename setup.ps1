#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Development environment setup script for The Cabin.

.DESCRIPTION
    This script validates the development environment and sets up the project:
    - Checks for required .NET SDK version
    - Restores NuGet packages
    - Runs an initial build to verify setup
    - Optionally runs tests

.PARAMETER RunTests
    If specified, runs the test suite after setup.

.PARAMETER SkipBuild
    If specified, skips the initial build step.

.EXAMPLE
    .\setup.ps1
    Basic setup with build verification.

.EXAMPLE
    .\setup.ps1 -RunTests
    Setup with build and test run.
#>

param(
    [switch]$RunTests,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   The Cabin - Development Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Required .NET SDK version
$RequiredSdkMajorVersion = 9

# Step 1: Check .NET SDK
Write-Host "[1/4] Checking .NET SDK..." -ForegroundColor Yellow

try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ".NET SDK not found"
    }
    
    $majorVersion = [int]($dotnetVersion.Split('.')[0])
    
    if ($majorVersion -lt $RequiredSdkMajorVersion) {
        Write-Host "  ERROR: .NET SDK $RequiredSdkMajorVersion.0 or higher is required." -ForegroundColor Red
        Write-Host "  Current version: $dotnetVersion" -ForegroundColor Red
        Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/$RequiredSdkMajorVersion.0" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "  .NET SDK $dotnetVersion found" -ForegroundColor Green
}
catch {
    Write-Host "  ERROR: .NET SDK is not installed or not in PATH." -ForegroundColor Red
    Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/$RequiredSdkMajorVersion.0" -ForegroundColor Red
    exit 1
}

# Step 2: Restore NuGet packages
Write-Host ""
Write-Host "[2/4] Restoring NuGet packages..." -ForegroundColor Yellow

$restoreResult = dotnet restore --verbosity minimal 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Package restore failed." -ForegroundColor Red
    Write-Host $restoreResult -ForegroundColor Red
    exit 1
}
Write-Host "  Packages restored successfully" -ForegroundColor Green

# Step 3: Build the solution
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "[3/4] Building solution..." -ForegroundColor Yellow
    
    $buildResult = dotnet build --no-restore --configuration Debug --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: Build failed." -ForegroundColor Red
        Write-Host $buildResult -ForegroundColor Red
        exit 1
    }
    Write-Host "  Build completed successfully" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "[3/4] Skipping build (--SkipBuild specified)" -ForegroundColor Gray
}

# Step 4: Run tests (optional)
if ($RunTests) {
    Write-Host ""
    Write-Host "[4/4] Running tests..." -ForegroundColor Yellow
    
    $testResult = dotnet test --no-build --configuration Debug --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  WARNING: Some tests failed." -ForegroundColor Yellow
        Write-Host $testResult -ForegroundColor Yellow
    }
    else {
        Write-Host "  All tests passed" -ForegroundColor Green
    }
}
else {
    Write-Host ""
    Write-Host "[4/4] Skipping tests (use -RunTests to include)" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor White
Write-Host "  - Run the console app: dotnet run --project src/TheCabin.Console" -ForegroundColor Gray
Write-Host "  - Run tests: dotnet test" -ForegroundColor Gray
Write-Host "  - Build & test with coverage: .\build-and-test.ps1 -WithCoverage" -ForegroundColor Gray
Write-Host ""
