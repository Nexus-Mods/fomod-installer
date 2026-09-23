# Sign every .exe and .dll under a directory that isn't already validly signed,
# then check that everything under it is signed.
#
#     ./scripts/sign-package.ps1 <directory> [-Publisher <name>]
#
# Needs CodeSignTool from download-codesigntool.ps1 and the ES_* credentials in
# the environment.

param (
  [Parameter(Mandatory = $true, Position = 0)]
  [string] $Directory,

  [string] $Publisher = 'Black Tree Gaming'
)

Set-StrictMode -Version 'Latest'
$ErrorActionPreference = 'Stop'

function Get-Binaries {
  Get-ChildItem -Path $Directory -Recurse -File -Include '*.exe', '*.dll' | Sort-Object FullName
}

$binaries = @(Get-Binaries)
if ($binaries.Count -eq 0) {
  Write-Host "::error::No .exe or .dll found under '$Directory'."
  exit 1
}

$signer = Join-Path $PSScriptRoot 'sign-windows.mjs'
$signed = @()

foreach ($item in $binaries) {
  $signature = Get-AuthenticodeSignature -FilePath $item.FullName
  if ($signature.Status -eq 'Valid') {
    Write-Host "skip  $($item.Name), already signed by $($signature.SignerCertificate.Subject)"
    continue
  }

  Write-Host "sign  $($item.Name)"
  & node $signer $item.FullName
  if ($LASTEXITCODE -ne 0) {
    Write-Host "::error::Signing $($item.FullName) failed."
    exit 1
  }
  $signed += $item.FullName
}

$failed = $false

# Files we signed must be signed by us and timestamped.
foreach ($path in $signed) {
  $signature = Get-AuthenticodeSignature -FilePath $path
  if ($signature.Status -ne 'Valid') {
    Write-Host "::error::$path is not validly signed: $($signature.StatusMessage)"
    $failed = $true
  }
  elseif ($signature.SignerCertificate.Subject -notlike "*$Publisher*") {
    Write-Host "::error::$path is signed by $($signature.SignerCertificate.Subject), not '$Publisher'."
    $failed = $true
  }
  elseif ($null -eq $signature.TimeStamperCertificate) {
    Write-Host "::error::$path has no timestamp."
    $failed = $true
  }
}

# Nothing under the directory is left unsigned.
foreach ($item in Get-Binaries) {
  if ((Get-AuthenticodeSignature -FilePath $item.FullName).Status -ne 'Valid') {
    Write-Host "::error::$($item.FullName) is not signed."
    $failed = $true
  }
}

if ($failed) {
  exit 1
}
Write-Host "Signed $($signed.Count) of $($binaries.Count) binaries under '$Directory'."
exit 0
