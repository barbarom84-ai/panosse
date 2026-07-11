# Script de publication Panosse (WinUI 3)
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$projectPath = "src/Panosse.WinUI/Panosse.WinUI.csproj"
$publishDir = "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Publication Panosse WinUI           " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $projectPath)) {
    Write-Host "ERREUR: $projectPath introuvable." -ForegroundColor Red
    exit 1
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Compilation et publication..." -ForegroundColor Yellow
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    -p:Platform=x64 `
    -p:SelfContained=true `
    -p:PublishTrimmed=true `
    -p:TrimMode=partial `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERREUR: publication echouee." -ForegroundColor Red
    exit 1
}

$exePath = Join-Path $publishDir "Panosse.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERREUR: Panosse.exe introuvable dans $publishDir" -ForegroundColor Red
    exit 1
}

$sizeMb = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
Write-Host ""
Write-Host "Publication reussie." -ForegroundColor Green
Write-Host "  Fichier : $((Resolve-Path $exePath).Path)" -ForegroundColor White
Write-Host "  Taille  : $sizeMb Mo" -ForegroundColor White
Write-Host ""
