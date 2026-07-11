# Publication portable single-file Panosse (WinUI 3)
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$projectPath = "src/Panosse.WinUI/Panosse.WinUI.csproj"
$publishDir = "publish"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Publication portable Panosse          " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $projectPath)) {
    Write-Host "ERREUR: $projectPath introuvable." -ForegroundColor Red
    exit 1
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

Write-Host "Compilation single-file self-contained..." -ForegroundColor Yellow
dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    -p:Platform=x64 `
    -p:SelfContained=true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:WindowsAppSDKSelfContained=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:PublishTrimmed=false `
    -p:DebugType=none `
    -p:DebugSymbols=false `
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
$hash = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash

Write-Host ""
Write-Host "Publication reussie." -ForegroundColor Green
Write-Host "  Fichier : $((Resolve-Path $exePath).Path)" -ForegroundColor White
Write-Host "  Taille  : $sizeMb Mo" -ForegroundColor White
Write-Host "  SHA256  : $hash" -ForegroundColor White
Write-Host ""
Write-Host "Version portable: un seul executable, aucune installation requise." -ForegroundColor Cyan
Write-Host ""

Set-Content -Path (Join-Path $publishDir "SHA256SUMS.txt") -Value "$hash  Panosse.exe" -Encoding ascii
