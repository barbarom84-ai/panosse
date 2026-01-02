# Script pour creer la release GitHub v2.0.0
# Necessite gh CLI : https://cli.github.com/

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   CREATION RELEASE GITHUB v2.0.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verifier gh CLI
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "ERREUR : gh CLI non installe" -ForegroundColor Red
    Write-Host "Installez gh CLI depuis : https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Verifier les fichiers
$exeFile = "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe"
$installerFile = "installer\Panosse-Setup-v2.0.0.exe"

if (-not (Test-Path $exeFile)) {
    Write-Host "ERREUR : $exeFile introuvable" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $installerFile)) {
    Write-Host "ERREUR : $installerFile introuvable" -ForegroundColor Red
    exit 1
}

Write-Host "Fichiers detectes :" -ForegroundColor Green
Write-Host "  - Panosse.exe : $([math]::Round((Get-Item $exeFile).Length / 1MB, 2)) Mo" -ForegroundColor White
Write-Host "  - Installateur : $([math]::Round((Get-Item $installerFile).Length / 1MB, 2)) Mo" -ForegroundColor White
Write-Host ""

# Notes de version
$releaseNotes = @"
# 🧹 Panosse v2.0.0 - Mémoire Sélective

## 🆕 Nouveautés majeures

### 🎯 Raccourci global **Ctrl+Alt+P**
- Nettoyage instantané en arrière-plan depuis n'importe quelle application
- Notification Toast avec espace libéré
- Fonctionne même quand la fenêtre est fermée

### 🔔 Icône System Tray intelligente
- **Icône verte (propre)** : Tout va bien, PC propre
- **Icône rouge (sale)** : Téléchargements encombrés (> 5 Go)
- Menu contextuel : Ouvrir, Nettoyer, Info, Quitter
- Double-clic : Ouvrir Panosse

### 🧠 Surveillance automatique
- Vérification horaire du dossier Téléchargements
- Alerte visuelle si > 5 Go ou fichiers anciens (> 30 jours)
- Clic droit → "Pourquoi l'icône est rouge ?" pour détails

### ⚙️ Lancement au démarrage (optionnel)
- Option cochée par défaut dans l'installateur
- Garantit que **Ctrl+Alt+P** est toujours actif
- Clé de registre : `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

---

## 📦 Fichiers disponibles

### 1️⃣ **Panosse.exe** (Portable)
- Exécutable unique, aucune installation requise
- Taille : ~76 Mo (runtime .NET 8.0 inclus)
- Double-clic pour lancer immédiatement

### 2️⃣ **Panosse-Setup-v2.0.0.exe** (Installateur)
- Installation complète avec raccourcis
- Option "Lancer au démarrage de Windows"
- Désinstallation propre via Paramètres Windows
- Taille : ~73 Mo

---

## ✨ Fonctionnalités complètes

### Nettoyage automatique
- ✅ Corbeille Windows
- ✅ Cache navigateurs (Edge, Chrome, Firefox)
- ✅ Fichiers temporaires système (%TEMP%)
- ✅ Logs Windows (C:\Windows\Logs)
- ✅ Cache miniatures (Thumbnails)
- ✅ Téléchargements anciens (.exe, .msi > 14 jours)
- ✅ Registre Windows (RunMRU, RecentDocs)

### Interface moderne
- Barre de menu professionnelle (Fichier, Outils, Aide)
- Progress bar détaillée avec liste des tâches
- Animations fluides (fade-in, bounce)
- Vérification automatique des mises à jour
- Panneau "À propos" avec version

### Intégration Windows
- System Tray permanent
- Raccourci global **Ctrl+Alt+P**
- Notifications Toast
- Menu contextuel complet
- Lancement au démarrage (optionnel)

---

## 🚀 Installation

### Méthode 1 : Installateur (Recommandé)
1. Téléchargez **Panosse-Setup-v2.0.0.exe**
2. Exécutez l'installateur (droits admin requis)
3. Cochez "Lancer au démarrage" pour profiter de **Ctrl+Alt+P**
4. Profitez !

### Méthode 2 : Portable
1. Téléchargez **Panosse.exe**
2. Double-cliquez pour lancer
3. Aucune installation, aucun résidu

---

## 💡 Utilisation

### Nettoyage manuel
1. Ouvrez Panosse
2. Cliquez sur "Passer la panosse"
3. Observez le nettoyage en temps réel

### Nettoyage instantané
- Appuyez sur **Ctrl+Alt+P** n'importe quand
- Panosse nettoie en arrière-plan
- Notification Toast à la fin

### Surveillance
- Icône System Tray change de couleur si besoin
- Clic droit → "Pourquoi l'icône est rouge ?"
- Détails sur l'encombrement du dossier Téléchargements

---

## 📋 Configuration requise

- **OS** : Windows 10 / 11 (64-bit)
- **RAM** : 2 Go minimum
- **Espace disque** : 100 Mo
- **Droits** : Administrateur (pour nettoyage système)

---

## 🔒 Sécurité

- ✅ Nettoyage uniquement de fichiers temporaires et obsolètes
- ✅ Aucun fichier système critique touché
- ✅ Gestion robuste des erreurs (accès refusés silencieux)
- ✅ Open source : code vérifiable sur GitHub

---

## 📝 Notes de version

### v2.0.0 (2025-01-02)
- 🆕 Raccourci global Ctrl+Alt+P
- 🆕 Icône System Tray intelligente (propre/sale)
- 🆕 Surveillance automatique Téléchargements
- 🆕 Option lancement au démarrage
- 🆕 Menu contextuel System Tray
- 🆕 Notification Toast
- 🆕 Barre de menu professionnelle
- 🔧 Amélioration interface utilisateur
- 🔧 Optimisation performance
- 🐛 Corrections bugs mineurs

### v1.1.1 (2024-12-XX)
- ✨ Vérification automatique des mises à jour
- ✨ Téléchargement et installation auto
- 🔧 Interface "À propos" améliorée

### v1.0.0 (2024-12-XX)
- 🎉 Version initiale
- ✨ Nettoyage automatique complet
- ✨ Progress bar détaillée
- ✨ Animations

---

## 🆘 Support

- **GitHub** : [barbarom84-ai/panosse](https://github.com/barbarom84-ai/panosse)
- **Issues** : [Signaler un bug](https://github.com/barbarom84-ai/panosse/issues)
- **Documentation** : README.md dans le projet

---

## 📄 Licence

Open Source - Utilisation libre

---

## 🎉 Merci d'utiliser Panosse !

**La serpillère numérique qui garde votre PC tout propre ! 🧹✨**
"@

Write-Host "Creation de la release GitHub..." -ForegroundColor Yellow

# Creer la release avec gh CLI
gh release create v2.0.0 `
    --title "Panosse v2.0.0 - Memoire Selective" `
    --notes $releaseNotes `
    $exeFile#Panosse.exe `
    $installerFile#Panosse-Setup-v2.0.0.exe

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   RELEASE v2.0.0 CREEE AVEC SUCCES !" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "URL : https://github.com/barbarom84-ai/panosse/releases/tag/v2.0.0" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Assets telecharges :" -ForegroundColor Yellow
    Write-Host "  - Panosse.exe" -ForegroundColor Green
    Write-Host "  - Panosse-Setup-v2.0.0.exe" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "ERREUR lors de la creation de la release" -ForegroundColor Red
    Write-Host ""
    Write-Host "SOLUTION MANUELLE :" -ForegroundColor Yellow
    Write-Host "1. Ouvrez : https://github.com/barbarom84-ai/panosse/releases/new" -ForegroundColor White
    Write-Host "2. Choisissez le tag : v2.0.0" -ForegroundColor White
    Write-Host "3. Titre : Panosse v2.0.0 - Memoire Selective" -ForegroundColor White
    Write-Host "4. Copiez les notes de version ci-dessus" -ForegroundColor White
    Write-Host "5. Uploadez les 2 fichiers :" -ForegroundColor White
    Write-Host "   - $exeFile" -ForegroundColor Gray
    Write-Host "   - $installerFile" -ForegroundColor Gray
    Write-Host ""
}

