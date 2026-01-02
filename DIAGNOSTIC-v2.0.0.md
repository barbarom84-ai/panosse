# 🔧 Guide de diagnostic Panosse v2.0.0

## ❓ Problème signalé
"La version 2.0.0 ne se lance pas"

---

## ✅ Tests effectués automatiquement

### 1. Compilation
- ✅ **Mode Debug** : Pas d'erreurs
- ✅ **Mode Release** : Pas d'erreurs
- ✅ **Restauration des packages** : OK

### 2. Lancement des exécutables
- ✅ **Version Debug** : Se lance correctement
- ✅ **Version Release** : Se lance correctement

### 3. Ressources
- ✅ **panosse.ico** : Présent (125.95 KB)
- ✅ **panosse_propre.ico** : Présent (125.95 KB)
- ✅ **panosse_sale.ico** : Présent (154.99 KB)
- ✅ **Inclus dans .csproj** : Vérifié et corrigé

---

## 🔍 Scénarios possibles

### Scénario 1 : L'application ne démarre pas du tout
**Symptômes** :
- Double-clic sur Panosse.exe ne fait rien
- Aucune fenêtre n'apparaît
- Aucun message d'erreur

**Causes possibles** :
- .NET Runtime 8.0 manquant (peu probable si c'est un single-file avec runtime inclus)
- Antivirus bloquant l'exécution
- Fichier corrompu lors du téléchargement

**Solutions** :
```powershell
# Vérifier l'intégrité du fichier
Get-FileHash "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe" -Algorithm SHA256
# Devrait retourner : B7FDEAF45058486A0CD62125EBEDA3F3C170BE45E8EE92B1C549288B2A2BB6D9

# Lancer avec droits administrateur
Start-Process "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe" -Verb RunAs
```

---

### Scénario 2 : L'application crash au démarrage
**Symptômes** :
- Une fenêtre apparaît brièvement puis disparaît
- Message d'erreur Windows "L'application a cessé de fonctionner"
- Événement dans l'Observateur d'événements Windows

**Causes possibles** :
- Exception non gérée dans InitialiserSystemTray()
- Problème de chargement des icônes
- Conflit avec une autre instance de Panosse

**Solutions** :
```powershell
# Vérifier les logs d'événements Windows
Get-EventLog -LogName Application -Source "Application Error" -Newest 5 | Where-Object {$_.Message -like "*Panosse*"}

# Fermer toutes les instances existantes
taskkill /F /IM Panosse.exe

# Lancer en mode console pour voir les erreurs
# (nécessite de recompiler avec OutputType=Exe au lieu de WinExe)
```

---

### Scénario 3 : L'application se lance mais l'icône System Tray n'apparaît pas
**Symptômes** :
- La fenêtre principale apparaît
- Pas d'icône dans le System Tray
- Le raccourci Ctrl+Alt+P ne fonctionne pas

**Causes possibles** :
- NotifyIcon pas initialisé correctement
- Icônes non chargées (panosse.ico ou panosse_sale.ico manquants)
- System Tray désactivé dans Windows

**Solutions** :
1. Vérifier les ressources embarquées :
```powershell
# Les icônes doivent être embarquées en tant que Resource
# Vérifié dans Panosse.csproj (corrigé)
```

2. Tester le chargement manuel :
- Ouvrir Panosse
- Regarder si le menu "Fichier" → "Actualiser la détection" fonctionne

---

### Scénario 4 : Erreur "Impossible de charger le fichier ou l'assembly"
**Symptômes** :
- Message d'erreur avec "System.IO.FileNotFoundException"
- Mention de "System.Windows.Forms"

**Causes possibles** :
- Référence System.Windows.Forms manquante
- Conflit de versions .NET

**Solutions** :
```xml
<!-- Vérifier dans Panosse.csproj -->
<UseWindowsForms>true</UseWindowsForms>
```

---

### Scénario 5 : Erreur "Cette application nécessite une élévation"
**Symptômes** :
- Message d'erreur UAC
- L'application ne se lance pas sans droits admin

**Causes possibles** :
- app.manifest demande requireAdministrator

**Solutions** :
```powershell
# Lancer avec droits administrateur
Start-Process "Panosse.exe" -Verb RunAs
```

---

### Scénario 6 : L'installateur ne fonctionne pas
**Symptômes** :
- Panosse-Setup-v2.0.0.exe ne se lance pas
- Erreur "Impossible d'extraire les fichiers"

**Causes possibles** :
- Inno Setup corrompu
- Fichiers source manquants lors de la compilation

**Solutions** :
```powershell
# Recréer l'installateur
.\creer-installateur.ps1
```

---

## 🛠️ Actions de diagnostic à effectuer

### 1. Vérifier quelle version vous utilisez
```powershell
# Afficher les versions disponibles
Write-Host "Version Debug :"
if (Test-Path "bin\Debug\net8.0-windows\Panosse.exe") { Write-Host "  PRESENTE" -ForegroundColor Green } else { Write-Host "  ABSENTE" -ForegroundColor Red }

Write-Host "Version Release (non-publish) :"
if (Test-Path "bin\Release\net8.0-windows\win-x64\Panosse.exe") { Write-Host "  PRESENTE" -ForegroundColor Green } else { Write-Host "  ABSENTE" -ForegroundColor Red }

Write-Host "Version Release (publish - single file) :"
if (Test-Path "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe") { Write-Host "  PRESENTE" -ForegroundColor Green } else { Write-Host "  ABSENTE" -ForegroundColor Red }

Write-Host "Installateur :"
if (Test-Path "installer\Panosse-Setup-v2.0.0.exe") { Write-Host "  PRESENT" -ForegroundColor Green } else { Write-Host "  ABSENT" -ForegroundColor Red }
```

---

### 2. Lancer avec capture d'erreur
```powershell
# Créer un script de lancement avec log
$script = @"
`$ErrorActionPreference = 'Continue'
try {
    Start-Process '$PWD\bin\Release\net8.0-windows\win-x64\publish\Panosse.exe' -Wait -ErrorAction Stop
} catch {
    `$_ | Out-File -FilePath 'panosse_error.log' -Append
    Write-Host "ERREUR : `$_" -ForegroundColor Red
}
"@

$script | Out-File -FilePath "lancer_panosse.ps1" -Encoding UTF8
Write-Host "Script cree : lancer_panosse.ps1"
Write-Host "Executez-le pour voir les erreurs detaillees"
```

---

### 3. Vérifier les dépendances
```powershell
# Vérifier si .NET 8.0 Runtime est installé (normalement pas nécessaire pour single-file)
dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop.App"
```

---

### 4. Vérifier l'intégrité du fichier
```powershell
# SHA256 attendu pour Panosse.exe (v2.0.0)
$expectedHash = "B7FDEAF45058486A0CD62125EBEDA3F3C170BE45E8EE92B1C549288B2A2BB6D9"
$actualHash = (Get-FileHash "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe" -Algorithm SHA256).Hash

if ($actualHash -eq $expectedHash) {
    Write-Host "OK - Fichier integre" -ForegroundColor Green
} else {
    Write-Host "ERREUR - Fichier corrompu ou version differente" -ForegroundColor Red
    Write-Host "Attendu : $expectedHash"
    Write-Host "Obtenu  : $actualHash"
}
```

---

## 📋 Informations à fournir pour le diagnostic

Pour que je puisse vous aider efficacement, merci de me fournir :

### 1. Quel fichier ne fonctionne pas ?
- [ ] `bin\Debug\net8.0-windows\Panosse.exe`
- [ ] `bin\Release\net8.0-windows\win-x64\Panosse.exe`
- [ ] `bin\Release\net8.0-windows\win-x64\publish\Panosse.exe` (single-file)
- [ ] `installer\Panosse-Setup-v2.0.0.exe` (installateur)
- [ ] Application installée via l'installateur dans `C:\Program Files\Panosse\`

### 2. Quel est le comportement exact ?
- [ ] Rien ne se passe (aucune fenêtre, aucune erreur)
- [ ] Une fenêtre apparaît brièvement puis disparaît
- [ ] Message d'erreur Windows (précisez le message)
- [ ] L'application se lance mais certaines fonctions ne marchent pas (précisez)
- [ ] Autre (précisez)

### 3. Message d'erreur (si applicable)
```
[Copiez ici le message d'erreur exact]
```

### 4. Contexte
- [ ] C'est la première fois que j'essaie de lancer Panosse v2.0.0
- [ ] Panosse fonctionnait avant (version précédente)
- [ ] J'ai recompilé le projet moi-même
- [ ] J'utilise le fichier téléchargé depuis GitHub Release

### 5. Système d'exploitation
- Windows 10 ou Windows 11 ?
- Version (ex: Windows 11 23H2) ?
- 64-bit ?

---

## 🔧 Solution rapide : Recompiler proprement

Si vous avez des doutes, voici comment recompiler proprement :

```powershell
# 1. Fermer toutes les instances de Panosse
taskkill /F /IM Panosse.exe 2>$null

# 2. Nettoyer complètement
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "installer" -Recurse -Force -ErrorAction SilentlyContinue

# 3. Restaurer et recompiler
dotnet restore
dotnet clean
dotnet build -c Release

# 4. Publier en single-file
dotnet publish -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

# 5. Tester
Start-Process "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe"
```

---

## 📞 En attendant votre retour

J'ai effectué les corrections suivantes :
- ✅ Ajouté `panosse_propre.ico` dans le `.csproj`
- ✅ Recompilé le projet
- ✅ Vérifié que les versions Debug et Release fonctionnent

**En attendant vos précisions sur le problème exact, les fichiers actuels devraient fonctionner correctement.**

Si le problème persiste, merci de me fournir les informations demandées ci-dessus pour que je puisse identifier la cause exacte ! 🔍

