# Script pour créer manuellement la release v1.0.0 sur GitHub

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  CREATION DE LA RELEASE v1.0.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Ouvrir la page de création de release
$url = "https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.0.0&title=Panosse+v1.0.0"
Write-Host "Ouverture de la page GitHub..." -ForegroundColor Yellow
Start-Process $url

Write-Host ""
Write-Host "INSTRUCTIONS :" -ForegroundColor Green
Write-Host ""
Write-Host "1. Sur la page GitHub qui vient de s'ouvrir :" -ForegroundColor Yellow
Write-Host "   - Le tag 'v1.0.0' est déjà sélectionné" -ForegroundColor White
Write-Host "   - Le titre 'Panosse v1.0.0' est déjà rempli" -ForegroundColor White
Write-Host ""
Write-Host "2. Dans la description, copiez-collez ce texte :" -ForegroundColor Yellow
Write-Host ""
Write-Host "---------------------------------------------------" -ForegroundColor Gray

$description = @"
## 🧹 Panosse v1.0.0

**La serpillère numérique pour un PC tout propre !**

### 📦 Installation

Téléchargez ``Panosse-v1.0.0.exe`` ci-dessous et lancez-le.

**Aucune installation requise** - Version portable complète.

### ✨ Fonctionnalités

- 🗑️ Vidage de la corbeille
- 🧹 Nettoyage fichiers temporaires
- 🌐 Cache navigateurs (Chrome, Firefox, Edge)
- 📋 Nettoyage registre (RunMRU, RecentDocs)
- 📥 Suppression .exe/.msi anciens (Téléchargements)
- 📄 Nettoyage logs Windows
- 🖼️ Cache miniatures
- 📊 Progression détaillée avec animations
- 🔄 Mise à jour automatique

### ⚠️ Prérequis

- **Windows 10/11** (64-bit)
- **Droits administrateur** (certaines fonctions)
- **.NET 8.0** inclus (self-contained)

### 🔐 Checksum SHA256

``````
E60323F663490C66E92F6A0520B58EB9ABD65F4B053049C741C8EE8A3F80E2BF
``````
"@

Write-Host $description -ForegroundColor White
Write-Host ""
Write-Host "---------------------------------------------------" -ForegroundColor Gray
Write-Host ""
Write-Host "3. En bas de la page, uploadez les fichiers :" -ForegroundColor Yellow
Write-Host "   - release-manual\Panosse-v1.0.0.exe" -ForegroundColor Cyan
Write-Host "   - release-manual\SHA256SUMS.txt" -ForegroundColor Cyan
Write-Host ""
Write-Host "4. Cliquez sur 'Publish release'" -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  C'EST TOUT !" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Après publication, testez Panosse :" -ForegroundColor Cyan
Write-Host "  1. Lancez Panosse.exe" -ForegroundColor White
Write-Host "  2. Cliquez sur 'i' (À propos)" -ForegroundColor White
Write-Host "  3. Cliquez sur 'Vérifier les mises à jour'" -ForegroundColor White
Write-Host "  4. Vous devriez voir : 'Version à jour' !" -ForegroundColor Green
Write-Host ""

# Copier la description dans le presse-papiers si possible
try {
    $description | Set-Clipboard
    Write-Host "La description a été copiée dans le presse-papiers !" -ForegroundColor Green
    Write-Host "Faites Ctrl+V sur GitHub pour la coller." -ForegroundColor Yellow
} catch {
    # Ignore si Set-Clipboard n'est pas disponible
}

Write-Host ""
Write-Host "Appuyez sur Entrée pour ouvrir le dossier des fichiers..." -ForegroundColor Yellow
Read-Host

# Ouvrir le dossier contenant les fichiers
Start-Process "release-manual"

Write-Host ""
Write-Host "Dossier ouvert ! Glissez-déposez les 2 fichiers sur GitHub." -ForegroundColor Green
Write-Host ""

