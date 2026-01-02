# ⚡ Release rapide - Script automatique

## 🚀 Créer une release en 1 commande

Ce script crée un tag et déclenche automatiquement la release GitHub.

---

## 📝 Utilisation

### Option 1 : Script interactif

```powershell
cd "C:\Users\marco\Cursor Workplace\panosse"

# Le script vous demandera la version
.\release.ps1
```

### Option 2 : Avec paramètre

```powershell
.\release.ps1 -Version "1.0.0"
```

---

## 📦 Ce que fait le script

1. ✅ Vérifie que vous êtes sur la branche `main`
2. ✅ Vérifie qu'il n'y a pas de modifications non commitées
3. ✅ Pull les derniers changements depuis GitHub
4. ✅ Crée le tag avec le format `v{version}`
5. ✅ Pousse le tag vers GitHub
6. ✅ Affiche le lien vers le workflow en cours
7. ✅ Affiche le lien vers la future release

---

## 🎯 Exemple complet

```powershell
PS> .\release.ps1

╔═══════════════════════════════════════════╗
║   🚀 PANOSSE - RELEASE AUTOMATIQUE 🚀   ║
╚═══════════════════════════════════════════╝

Entrez le numéro de version (ex: 1.0.0): 1.0.0

✓ Branche: main
✓ Aucune modification non commitée
✓ Pull effectué
✓ Tag v1.0.0 créé
✓ Tag poussé vers GitHub

🎉 Release en cours de création !

📊 Suivre la progression :
   https://github.com/barbarom84-ai/panosse/actions

📦 Release disponible dans ~5 minutes :
   https://github.com/barbarom84-ai/panosse/releases/tag/v1.0.0
```

---

## 🔧 Installation

Le script `release.ps1` est déjà créé dans votre projet.

---

## ⚠️ Important

**Avant de lancer une release** :

1. ✅ Tous vos changements sont commitées
2. ✅ Le code compile sans erreur
3. ✅ Vous avez testé l'application
4. ✅ Vous êtes sur la branche `main`

---

## 📋 Workflow complet

```powershell
# 1. Faire vos modifications
# (éditer MainWindow.xaml.cs, etc.)

# 2. Tester localement
dotnet build -c Release

# 3. Commiter
& "C:\Program Files\Git\bin\git.exe" add .
& "C:\Program Files\Git\bin\git.exe" commit -m "Ajout de la fonctionnalité X"
& "C:\Program Files\Git\bin\git.exe" push

# 4. Créer la release automatiquement
.\release.ps1 -Version "1.0.1"

# 5. Attendre ~5 minutes

# 6. Télécharger et tester la release
```

---

## 🎨 Versions suggérées

### Version Initiale
```powershell
.\release.ps1 -Version "1.0.0"
```

### Correction de bugs
```powershell
.\release.ps1 -Version "1.0.1"
```

### Nouvelles fonctionnalités
```powershell
.\release.ps1 -Version "1.1.0"
```

### Changements majeurs
```powershell
.\release.ps1 -Version "2.0.0"
```

### Version beta
```powershell
.\release.ps1 -Version "1.0.0-beta"
```

---

**🚀 Script prêt à l'emploi !**

Lancez `.\release.ps1` pour créer votre première release automatique !

