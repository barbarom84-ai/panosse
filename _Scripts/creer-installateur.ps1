# Creation de l'installateur Panosse (WinUI 3)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Creation de l'installateur Panosse  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Etape 1/3 : Publication WinUI..." -ForegroundColor Yellow
& "$PSScriptRoot\publier.ps1"
if ($LASTEXITCODE -ne 0) { exit 1 }
Write-Host ""

Write-Host "Etape 2/3 : Verification d'Inno Setup..." -ForegroundColor Yellow
$innoSetupPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)

$isccPath = $innoSetupPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $isccPath) {
    Write-Host "ERREUR: Inno Setup 6 introuvable." -ForegroundColor Red
    exit 1
}
Write-Host "  OK: $isccPath" -ForegroundColor Green
Write-Host ""

Write-Host "Etape 3/3 : Compilation de l'installateur..." -ForegroundColor Yellow
& $isccPath "Panosse-Setup.iss"
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERREUR: compilation Inno Setup echouee." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Installateur cree dans .\installer\" -ForegroundColor Green
