# Sync Windows Uninstall DisplayVersion/DisplayName for Panosse (requires admin).
param(
    [string]$Version = "2.2.7"
)

$ErrorActionPreference = 'Stop'
$displayName = "Panosse $Version"
$id = '{8E5F4A3B-2D1C-4E9F-A7B6-3C8D9E2F1A4B}_is1'
$keys = @(
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$id",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\$id"
)

$updated = 0
foreach ($key in $keys) {
    if (-not (Test-Path -LiteralPath $key)) { continue }
    Set-ItemProperty -LiteralPath $key -Name DisplayVersion -Value $Version -Type String -Force
    Set-ItemProperty -LiteralPath $key -Name DisplayName -Value $displayName -Type String -Force
    $updated++
    Write-Host "Updated $key -> $displayName"
}

if ($updated -eq 0) {
    Write-Host "Aucune cle Uninstall Panosse trouvee (installateur Inno requis)." -ForegroundColor Yellow
    exit 1
}

Write-Host "OK ($updated cle(s))." -ForegroundColor Green
