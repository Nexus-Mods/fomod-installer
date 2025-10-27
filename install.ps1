#!/usr/bin/env pwsh
# Installation script for meta package
# Sets FOMOD_METAPACKAGE_INSTALL=1 to skip child package builds

Write-Host "Installing meta package with FOMOD_METAPACKAGE_INSTALL=1" -ForegroundColor Cyan
Write-Host "Child packages will skip their build steps" -ForegroundColor Cyan
Write-Host ""

$env:FOMOD_METAPACKAGE_INSTALL = '1'
yarn install

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Meta package installation complete!" -ForegroundColor Green
    Write-Host "The sub-packages are linked but not built." -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "Meta package installation failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}
