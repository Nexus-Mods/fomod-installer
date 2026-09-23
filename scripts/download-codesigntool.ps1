# Download and extract SSL.com's CodeSignTool into ./CodeSignTool.
#
#     ./scripts/download-codesigntool.ps1 [-Sandbox]
#
# -Sandbox points it at SSL.com's test environment, which spends no signing credits.

param (
  [switch] $Sandbox
)

Set-StrictMode -Version 'Latest'
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Version = 'v1.3.2'
$Asset = "CodeSignTool-$Version-windows.zip"

$rootDir = Resolve-Path (Join-Path $PSScriptRoot '..')
$downloadUrl = "https://github.com/SSLcom/CodeSignTool/releases/download/$Version/$Asset"
$downloadedFile = Join-Path $rootDir 'CodeSignTool.zip'
$extractFolder = Join-Path $rootDir 'CodeSignTool'

if (Test-Path $extractFolder) {
  Remove-Item -Path $extractFolder -Recurse -Force
}

# The zip is kept so CI can cache it.
if (!(Test-Path $downloadedFile -PathType Leaf)) {
  Write-Host "Downloading $downloadUrl"
  Invoke-WebRequest -OutFile $downloadedFile $downloadUrl
}

Expand-Archive -Path $downloadedFile -DestinationPath $extractFolder -Force

# Flatten a single nested folder so CodeSignTool.bat sits in $extractFolder.
$folders = @(Get-ChildItem $extractFolder -Directory)
if ($folders.Count -eq 1) {
  Get-ChildItem -Path $folders[0].FullName | Move-Item -Destination $extractFolder
  Remove-Item -Path $folders[0].FullName -Force
}

if ($Sandbox) {
  $propertiesFile = Join-Path $extractFolder 'conf/code_sign_tool.properties'
  $null = New-Item -Path $propertiesFile -ItemType File -Force
  Add-Content -Path $propertiesFile -Value 'CLIENT_ID=qOUeZCCzSqgA93acB3LYq6lBNjgZdiOxQc-KayC3UMw'
  Add-Content -Path $propertiesFile -Value 'OAUTH2_ENDPOINT=https://oauth-sandbox.ssl.com/oauth2/token'
  Add-Content -Path $propertiesFile -Value 'CSC_API_ENDPOINT=https://cs-try.ssl.com'
  Add-Content -Path $propertiesFile -Value 'TSA_URL=http://ts.ssl.com'
}

# The bundled JDK 11.0.2 doesn't trust SSL.com TLS RSA Root CA 2022, which
# cs.ssl.com has chained to since 2026-09-22. Add it to that JDK's cacerts.
$keytool = Join-Path $extractFolder 'jdk-11.0.2/bin/keytool.exe'
$root = Join-Path $PSScriptRoot 'certs/SSLcomTLSRSARootCA2022.pem'
& $keytool -importcert -noprompt -cacerts -storepass changeit -alias sslcom-tls-rsa-root-2022 -file $root
if ($LASTEXITCODE -ne 0) {
  Write-Host "::error::Could not add $root to CodeSignTool's JDK."
  exit 1
}

$bat = Join-Path $extractFolder 'CodeSignTool.bat'
if (!(Test-Path $bat -PathType Leaf)) {
  Write-Host "::error::CodeSignTool.bat is not at $bat."
  exit 1
}
Write-Host "CodeSignTool $Version ready at $bat"
exit 0
