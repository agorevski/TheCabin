# The Cabin - Build and Test Script
# This script builds the project, runs all tests, and provides a summary

param(
    [switch]$IncludeMauiWindows = $false,
    [switch]$WithCoverage = $false,
    [int]$CoverageThreshold = 50
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  The Cabin - Build and Test Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ErrorActionPreference = "Continue"
$buildSuccess = $true
$testSuccess = $true
$mauiWindowsSuccess = $true
$coverageSuccess = $true

# Step 1: Clean previous builds
Write-Host "[1/5] Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean src/TheCabin.Core/TheCabin.Core.csproj --configuration Release --verbosity quiet 2>$null
dotnet clean src/TheCabin.Infrastructure/TheCabin.Infrastructure.csproj --configuration Release --verbosity quiet 2>$null
dotnet clean tests/TheCabin.Core.Tests/TheCabin.Core.Tests.csproj --configuration Release --verbosity quiet 2>$null
dotnet clean tests/TheCabin.Infrastructure.Tests/TheCabin.Infrastructure.Tests.csproj --configuration Release --verbosity quiet 2>$null
Write-Host "√ Clean successful" -ForegroundColor Green
Write-Host ""

# Step 2: Restore NuGet packages
Write-Host "[2/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Restore failed" -ForegroundColor Red
    $buildSuccess = $false
} else {
    Write-Host "√ Restore successful" -ForegroundColor Green
}
Write-Host ""

# Step 3: Build Core library
Write-Host "[3/5] Building TheCabin.Core..." -ForegroundColor Yellow
dotnet build src/TheCabin.Core/TheCabin.Core.csproj --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Core build failed" -ForegroundColor Red
    $buildSuccess = $false
} else {
    Write-Host "√ Core build successful" -ForegroundColor Green
}
Write-Host ""

# Step 4: Build Infrastructure library
Write-Host "[4/5] Building TheCabin.Infrastructure..." -ForegroundColor Yellow
dotnet build src/TheCabin.Infrastructure/TheCabin.Infrastructure.csproj --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Infrastructure build failed" -ForegroundColor Red
    $buildSuccess = $false
} else {
    Write-Host "√ Infrastructure build successful" -ForegroundColor Green
}
Write-Host ""

# Step 5: Run Tests
Write-Host "[5/5] Running all tests..." -ForegroundColor Yellow

# Clean test results
if (Test-Path "./TestResults") {
    Remove-Item -Recurse -Force "./TestResults"
}

# Prepare test arguments
$testArgs = @("--configuration", "Release", "--verbosity", "quiet")
if ($WithCoverage) {
    $testArgs += @('--collect:"XPlat Code Coverage"', "--results-directory", "./TestResults", "--settings", "tests/coverlet.runsettings")
}

# Run Core Tests
Write-Host "  Running TheCabin.Core.Tests..." -ForegroundColor Gray
if ($WithCoverage) {
    dotnet test tests/TheCabin.Core.Tests/TheCabin.Core.Tests.csproj --configuration Release --verbosity quiet --collect:"XPlat Code Coverage" --results-directory ./TestResults
} else {
    dotnet test tests/TheCabin.Core.Tests/TheCabin.Core.Tests.csproj --configuration Release --verbosity quiet
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Core Tests failed or had errors" -ForegroundColor Red
    $testSuccess = $false
} else {
    Write-Host "√ Core Tests completed" -ForegroundColor Green
}

# Run Infrastructure Tests
Write-Host "  Running TheCabin.Infrastructure.Tests..." -ForegroundColor Gray
if ($WithCoverage) {
    dotnet test tests/TheCabin.Infrastructure.Tests/TheCabin.Infrastructure.Tests.csproj --configuration Release --verbosity quiet --collect:"XPlat Code Coverage" --results-directory ./TestResults
} else {
    dotnet test tests/TheCabin.Infrastructure.Tests/TheCabin.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
}
if ($LASTEXITCODE -ne 0) {
    Write-Host "X Infrastructure Tests failed or had errors" -ForegroundColor Red
    $testSuccess = $false
} else {
    Write-Host "√ Infrastructure Tests completed"-ForegroundColor Green
}
Write-Host ""

# Check Coverage Threshold
if ($WithCoverage -and $testSuccess) {
    Write-Host "Checking code coverage..." -ForegroundColor Yellow
    $coverageFiles = Get-ChildItem -Recurse -Path "./TestResults" -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue
    
    if ($coverageFiles) {
        $totalLineCoverage = 0
        $fileCount = 0
        
        foreach ($file in $coverageFiles) {
            $xml = [xml](Get-Content $file.FullName)
            $lineRate = [decimal]$xml.coverage.'line-rate'
            if ($lineRate -gt 0) {
                $totalLineCoverage += $lineRate * 100
                $fileCount++
            }
        }
        
        if ($fileCount -gt 0) {
            $averageCoverage = [math]::Round($totalLineCoverage / $fileCount, 2)
            Write-Host "  Line Coverage: $averageCoverage% (Threshold: $CoverageThreshold%)" -ForegroundColor Gray
            
            if ($averageCoverage -lt $CoverageThreshold) {
                Write-Host "X Coverage below threshold ($averageCoverage% < $CoverageThreshold%)" -ForegroundColor Red
                $coverageSuccess = $false
            } else {
                Write-Host "√ Coverage meets threshold" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "  No coverage files found" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Optional Step 6: Build MAUI Windows App
if ($IncludeMauiWindows) {
    Write-Host "[6/6] Building MAUI Windows app..." -ForegroundColor Yellow
    dotnet build src/TheCabin.Maui/TheCabin.Maui.csproj -f net9.0-windows10.0.19041.0 --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "X MAUI Windows build failed" -ForegroundColor Red
        $mauiWindowsSuccess = $false
    } else {
        Write-Host "√ MAUI Windows build successful" -ForegroundColor Green
    }
    Write-Host ""
}

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

if ($WithCoverage) {
    if ($coverageSuccess) {
        Write-Host "√ Coverage: PASSED (>= $CoverageThreshold%)" -ForegroundColor Green
    } else {
        Write-Host "X Coverage: FAILED (< $CoverageThreshold%)" -ForegroundColor Red
    }
}

if ($IncludeMauiWindows) {
    if ($mauiWindowsSuccess) {
        Write-Host "√ MAUI Windows: SUCCESS" -ForegroundColor Green
    } else {
        Write-Host "X MAUI Windows: FAILED" -ForegroundColor Red
    }
}

Write-Host ""
if (-not $IncludeMauiWindows) {
    Write-Host "Note: To build MAUI Windows app, use: .\build-and-test.ps1 -IncludeMauiWindows" -ForegroundColor Cyan
}
if (-not $WithCoverage) {
    Write-Host "Note: To run with coverage, use: .\build-and-test.ps1 -WithCoverage -CoverageThreshold 50" -ForegroundColor Cyan
}
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Return exit code
$exitCode = 0
if (-not $buildSuccess) { $exitCode = 1 }
if (-not $testSuccess) { $exitCode = 1 }
if ($WithCoverage -and -not $coverageSuccess) { $exitCode = 1 }
if ($IncludeMauiWindows -and -not $mauiWindowsSuccess) { $exitCode = 1 }

if ($exitCode -eq 0) {
    Write-Host "Done! All checks passed." -ForegroundColor Green
} else {
    Write-Host "Done with errors. See above for details." -ForegroundColor Red
}

exit $exitCode
