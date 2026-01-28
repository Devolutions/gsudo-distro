#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Build script for gsudo and UniElevate variants
.DESCRIPTION
    This script builds both the vanilla gsudo and the UniElevate variant from the same codebase.
.PARAMETER Variant
    Specify which variant to build: 'gsudo', 'UniElevate', or 'All' (default)
.PARAMETER Configuration
    Build configuration: 'Debug' or 'Release' (default)
.PARAMETER Clean
    Clean before building
#>

param(
    [Parameter()]
    [ValidateSet('gsudo', 'UniElevate', 'All')]
    [string]$Variant = 'All',
    
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [Parameter()]
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot "src\gsudo\gsudo.csproj"

function Build-Variant {
    param(
        [string]$VariantName,
        [string]$BuildVariant,
        [string]$OutputName
    )
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Building $VariantName" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
    
    $buildParams = @(
        'build'
        $projectPath
        '-c', $Configuration
        '--nologo'
    )
    
    if ($Clean) {
        Write-Host "Cleaning..." -ForegroundColor Yellow
        & dotnet clean $projectPath -c $Configuration --nologo
    }
    
    if ($BuildVariant) {
        $buildParams += "-p:BuildVariant=$BuildVariant"
    }
    
    Write-Host "Command: dotnet $($buildParams -join ' ')" -ForegroundColor Gray
    & dotnet @buildParams
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed for $VariantName"
        exit $LASTEXITCODE
    }
    
    Write-Host "`n✓ $VariantName build completed successfully" -ForegroundColor Green
}

# Build requested variants
if ($Variant -eq 'All' -or $Variant -eq 'gsudo') {
    Build-Variant -VariantName "gsudo (vanilla)" -BuildVariant $null -OutputName "gsudo"
}

if ($Variant -eq 'All' -or $Variant -eq 'UniElevate') {
    Build-Variant -VariantName "UniElevate" -BuildVariant "UniElevate" -OutputName "UniGetUI Elevator"
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "All builds completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`nOutput locations:"
Write-Host "  gsudo:        src\gsudo\bin\$Configuration\" -ForegroundColor Cyan
if ($Variant -eq 'All' -or $Variant -eq 'UniElevate') {
    Write-Host "  UniElevate:   src\gsudo\bin\$Configuration\" -ForegroundColor Cyan
}
