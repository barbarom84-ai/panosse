# Script de publication de Panosse
# Ce script compile l'application en mode Release en un seul fichier .exe

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Publication de Panosse en cours...  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Nettoyer les anciennes publications
Write-Host "1. Nettoyage des anciennes publications..." -ForegroundColor Yellow
if (Test-Path ".\publish") {
    Remove-Item ".\publish" -Recurse -Force
}
Write-Host "   ✓ Nettoyage terminé" -ForegroundColor Green
Write-Host ""

# Publier l'application
Write-Host "2. Compilation en mode Release (Single File)..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ Compilation réussie !" -ForegroundColor Green
    Write-Host ""
    
    # Afficher les informations sur le fichier généré
    Write-Host "3. Fichier généré :" -ForegroundColor Yellow
    $exePath = ".\publish\Panosse.exe"
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
        Write-Host "   Emplacement : $($fileInfo.FullName)" -ForegroundColor White
        Write-Host "   Taille      : $fileSizeMB Mo" -ForegroundColor White
        Write-Host ""
        
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "   Publication terminée avec succès !  " -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Le fichier Panosse.exe est prêt à être distribué !" -ForegroundColor Cyan
        Write-Host "Il contient tout ce dont il a besoin pour fonctionner." -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Pour tester : " -ForegroundColor Yellow -NoNewline
        Write-Host "Start-Process '.\publish\Panosse.exe' -Verb RunAs" -ForegroundColor White
    }
} else {
    Write-Host "   ✗ Erreur lors de la compilation" -ForegroundColor Red
    exit 1
}

