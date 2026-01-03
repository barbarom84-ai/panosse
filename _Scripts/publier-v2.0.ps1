# ============================================
# Script de publication Panosse v2.0.0
# ============================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   PUBLICATION PANOSSE v2.0.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Étape 1 : Nettoyage
Write-Host "Étape 1/4 : Nettoyage des anciens builds..." -ForegroundColor Yellow
if (Test-Path "bin\Release") {
    Remove-Item -Recurse -Force "bin\Release" -ErrorAction SilentlyContinue
}
if (Test-Path "obj\Release") {
    Remove-Item -Recurse -Force "obj\Release" -ErrorAction SilentlyContinue
}
Write-Host "  ✓ Nettoyage terminé" -ForegroundColor Green
Write-Host ""

# Étape 2 : Restauration des dépendances
Write-Host "Étape 2/4 : Restauration des dépendances..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Erreur lors de la restauration" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Dépendances restaurées" -ForegroundColor Green
Write-Host ""

# Étape 3 : Compilation en mode Release
Write-Host "Étape 3/4 : Compilation Release..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Erreur lors de la compilation" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Compilation réussie" -ForegroundColor Green
Write-Host ""

# Étape 4 : Publication Single File
Write-Host "Étape 4/4 : Publication Single File..." -ForegroundColor Yellow
dotnet publish -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Erreur lors de la publication" -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Publication réussie" -ForegroundColor Green
Write-Host ""

# Résumé
Write-Host "========================================" -ForegroundColor Green
Write-Host "   PUBLICATION TERMINÉE !" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$exePath = "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe"
if (Test-Path $exePath) {
    $fileSize = [math]::Round((Get-Item $exePath).Length / 1MB, 2)
    Write-Host "Exécutable généré :" -ForegroundColor Cyan
    Write-Host "  Chemin : $exePath" -ForegroundColor White
    Write-Host "  Taille : $fileSize Mo" -ForegroundColor White
    Write-Host ""
    
    # Calcul du SHA256
    Write-Host "Calcul du SHA256..." -ForegroundColor Yellow
    $sha256 = (Get-FileHash -Path $exePath -Algorithm SHA256).Hash
    Write-Host "  SHA256 : $sha256" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Prêt pour la distribution !" -ForegroundColor Green
} else {
    Write-Host "Erreur : Fichier Panosse.exe introuvable" -ForegroundColor Red
}

Write-Host ""

