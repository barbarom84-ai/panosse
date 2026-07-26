# Integration test: simulates UpdateService + UpdateOrchestrator flow (v2.2.0 -> v2.2.1)
$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$currentExe = Join-Path $dir 'Panosse.exe'
$currentVersion = '2.2.0'
$repo = 'barbarom84-ai/panosse'

Write-Host "=== 1. Check for update (API GitHub) ===" -ForegroundColor Cyan
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -Headers @{ 'User-Agent' = 'Panosse-App/1.0' }
$tagName = $release.tag_name
$remoteVersion = $tagName.TrimStart('v')
if ([version]$remoteVersion -le [version]$currentVersion) { throw "Pas de MAJ detectee ($remoteVersion <= $currentVersion)" }
Write-Host "MAJ disponible: $tagName" -ForegroundColor Green

$exeAsset = $release.assets | Where-Object { $_.name -like 'Panosse-v*.exe' } | Select-Object -First 1
if (-not $exeAsset) { throw 'Asset portable introuvable' }
$sumsAsset = $release.assets | Where-Object { $_.name -eq 'SHA256SUMS.txt' } | Select-Object -First 1
if (-not $sumsAsset) { throw 'SHA256SUMS.txt introuvable' }

$sumsContent = (Invoke-WebRequest -Uri $sumsAsset.browser_download_url -Headers @{ 'User-Agent' = 'Panosse-App/1.0' }).Content
$expectedSha = ($sumsContent -split "`n" | Where-Object { $_ -match [regex]::Escape($exeAsset.name) } | Select-Object -First 1).Split(' ', 2)[0].Trim()
if ([string]::IsNullOrWhiteSpace($expectedSha)) { throw 'Checksum SHA256 introuvable' }
Write-Host "Checksum attendu: $expectedSha"

Write-Host "=== 2. Telechargement ===" -ForegroundColor Cyan
$downloaded = Join-Path $env:TEMP "Panosse-$tagName.exe"
Invoke-WebRequest -Uri $exeAsset.browser_download_url -OutFile $downloaded -Headers @{ 'User-Agent' = 'Panosse-App/1.0' }
$actualSha = (Get-FileHash -Path $downloaded -Algorithm SHA256).Hash
if ($actualSha -ne $expectedSha) { throw "SHA256 invalide: $actualSha" }
Write-Host "SHA256 OK" -ForegroundColor Green

Write-Host "=== 3. Remplacement (script MAJ) ===" -ForegroundColor Cyan
$old = "$currentExe.old"
if (Test-Path $old) { Remove-Item $old -Force }
Move-Item -Path $currentExe -Destination $old -Force
Move-Item -Path $downloaded -Destination $currentExe -Force
if (Test-Path $old) { Remove-Item $old -Force }

$newVersion = (Get-Item $currentExe).VersionInfo.FileVersion
Write-Host "Nouvelle version fichier: $newVersion" -ForegroundColor Green
if ($newVersion -notlike '2.2.1*') { throw "Version inattendue: $newVersion" }

Write-Host "=== 4. Lancement post-MAJ ===" -ForegroundColor Cyan
$p = Start-Process $currentExe -PassThru
Start-Sleep -Seconds 4
if ($p.HasExited) { throw "Echec lancement post-MAJ (exit $($p.ExitCode))" }
Write-Host "Post-MAJ LAUNCH OK PID $($p.Id)" -ForegroundColor Green
Stop-Process -Id $p.Id -Force
Write-Host "=== TEST REUSSI ===" -ForegroundColor Green
