#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs Git hooks for The Cabin project.

.DESCRIPTION
    Copies the pre-commit hook for secret detection to .git/hooks/

.EXAMPLE
    .\install-hooks.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "Installing Git hooks..." -ForegroundColor Cyan

$hooksSource = Join-Path $PSScriptRoot "hooks"
$hooksTarget = Join-Path $PSScriptRoot ".git\hooks"

if (-not (Test-Path $hooksTarget)) {
    New-Item -ItemType Directory -Path $hooksTarget -Force | Out-Null
}

# Copy pre-commit hook
$preCommitSource = Join-Path $hooksSource "pre-commit"
$preCommitTarget = Join-Path $hooksTarget "pre-commit"

if (Test-Path $preCommitSource) {
    Copy-Item -Path $preCommitSource -Destination $preCommitTarget -Force
    Write-Host "  Installed: pre-commit (secret detection)" -ForegroundColor Green
}
else {
    Write-Host "  WARNING: pre-commit hook source not found at $preCommitSource" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Git hooks installed successfully!" -ForegroundColor Green
Write-Host "The pre-commit hook will scan for secrets before each commit." -ForegroundColor Gray
