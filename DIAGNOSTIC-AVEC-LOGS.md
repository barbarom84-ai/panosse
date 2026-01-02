# 🔍 Diagnostic approfondi - Panosse v2.0.0

## ✅ SYSTÈME DE LOGGING INSTALLÉ

J'ai ajouté un **système de logging complet** pour identifier exactement où l'application plante.

---

## 📝 Fichiers de log

Deux fichiers sont créés sur votre **Bureau** :

### 1. **`panosse_debug.log`**
Trace détaillée du démarrage :
- Chaque étape du constructeur
- Chaque étape de MainWindow_Loaded
- Initialisation System Tray
- Enregistrement HotKey
- Vérification navigateurs

### 2. **`panosse_crash.log`**
Détails complets si crash :
- Message d'erreur exact
- Stack trace complet
- Inner exception (si présente)

---

## 🧪 COMMENT TESTER

### Méthode 1 : Script automatique (RECOMMANDÉ)
```powershell
.\test-avec-logs.ps1
```

Ce script va :
1. Nettoyer les anciens logs
2. Lancer Panosse
3. Attendre 5 secondes
4. Afficher les logs dans la console
5. Fermer Panosse

---

### Méthode 2 : Test manuel

1. **Supprimez les anciens logs du Bureau** (si présents) :
   - `panosse_debug.log`
   - `panosse_crash.log`

2. **Lancez Panosse** :
   ```powershell
   .\bin\Release\net8.0-windows\win-x64\Panosse.exe
   ```
   OU double-cliquez sur l'exécutable

3. **Si l'application crash** :
   - Allez sur votre Bureau
   - Ouvrez `panosse_debug.log` pour voir où ça s'arrête
   - Ouvrez `panosse_crash.log` pour voir l'erreur exacte

4. **Envoyez-moi le contenu des logs**

---

## 📊 RÉSULTAT DE MES TESTS

### ✅ Test automatique réussi !

```
[22:10:49.088] Constructeur - Début
[22:10:49.283] Constructeur - InitializeComponent OK
[22:10:49.283] Constructeur - Loaded event ajouté
[22:10:49.284] Constructeur - TaskList configuré
[22:10:49.284] Constructeur - Version définie: 2.0.0
[22:10:49.287] Constructeur - Fin (succès)
[22:10:49.472] MainWindow_Loaded - Début
[22:10:49.472] MainWindow_Loaded - Initialisation System Tray...
[22:10:49.553] MainWindow_Loaded - System Tray initialisé OK
[22:10:49.554] MainWindow_Loaded - Enregistrement HotKey...
[22:10:49.554] MainWindow_Loaded - HotKey enregistré OK
[22:10:49.554] MainWindow_Loaded - Vérification navigateurs...
[22:10:49.560] MainWindow_Loaded - Navigateurs trouvés: 1
[22:10:49.560] MainWindow_Loaded - Vérification mises à jour...
[22:10:49.601] MainWindow_Loaded - Fin (succès)
```

**Toutes les étapes se terminent avec succès dans mes tests !**

---

## ❓ POURQUOI ÇA NE FONCTIONNE PAS CHEZ VOUS ?

Plusieurs possibilités :

### 1. **Fichier exécutable différent**
- Vous utilisez peut-être une ancienne version compilée
- **Solution** : Recompilez proprement

```powershell
# Nettoyer complètement
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue

# Recompiler
dotnet build -c Release

# Tester
.\test-avec-logs.ps1
```

---

### 2. **Version single-file vs version normale**
- La version single-file (`publish\Panosse.exe`) peut avoir des problèmes de chargement de ressources
- La version normale (`win-x64\Panosse.exe`) fonctionne peut-être mieux

**Testez les deux** :

```powershell
# Version normale (avec DLLs séparées)
.\bin\Release\net8.0-windows\win-x64\Panosse.exe

# Version single-file (tout en un)
.\bin\Release\net8.0-windows\win-x64\publish\Panosse.exe
```

---

### 3. **Antivirus bloquant silencieusement**
- Windows Defender ou un antivirus peut bloquer l'exécution
- **Solution** : Vérifiez les logs Windows Defender

```powershell
# Vérifier les événements de sécurité
Get-WinEvent -LogName "Microsoft-Windows-Windows Defender/Operational" -MaxEvents 10 | Where-Object {$_.Message -like "*Panosse*"}
```

---

### 4. **Problème de .NET Runtime**
- Même avec single-file, il peut y avoir des dépendances manquantes
- **Solution** : Vérifiez que .NET 8.0 est bien installé

```powershell
dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop.App"
```

Si pas installé :
```powershell
# Télécharger .NET 8.0 Desktop Runtime
Start-Process "https://dotnet.microsoft.com/download/dotnet/8.0"
```

---

### 5. **Droits administrateur manquants**
- L'application demande des droits admin (voir `app.manifest`)
- **Solution** : Lancez explicitement avec droits admin

```powershell
Start-Process "bin\Release\net8.0-windows\win-x64\Panosse.exe" -Verb RunAs
```

---

## 🎯 ACTION IMMÉDIATE

**Faites ceci maintenant** :

1. **Lancez le script de test** :
   ```powershell
   .\test-avec-logs.ps1
   ```

2. **Si ça échoue** :
   - Ouvrez `panosse_debug.log` sur votre Bureau
   - Ouvrez `panosse_crash.log` sur votre Bureau (si existe)
   - **COPIEZ-MOI LE CONTENU EXACT**

3. **Si les logs n'existent pas** :
   - Le crash est TRÈS précoce (avant même le log)
   - Testez avec droits admin :
   ```powershell
   Start-Process "bin\Release\net8.0-windows\win-x64\Panosse.exe" -Verb RunAs
   ```

---

## 📸 INFORMATIONS UTILES À ME FOURNIR

Pour que je puisse vous aider efficacement, merci de me dire :

### 1. Quel fichier testez-vous ?
```
[ ] bin\Debug\net8.0-windows\Panosse.exe
[ ] bin\Release\net8.0-windows\win-x64\Panosse.exe
[ ] bin\Release\net8.0-windows\win-x64\publish\Panosse.exe
[ ] installer\Panosse-Setup-v2.0.0.exe
[ ] Application installée dans C:\Program Files\Panosse\
```

### 2. Que se passe-t-il exactement ?
```
[ ] Rien (aucune fenêtre, aucun processus)
[ ] Une fenêtre apparaît brièvement puis disparaît
[ ] Un message d'erreur Windows (lequel ?)
[ ] L'icône System Tray apparaît mais pas la fenêtre
[ ] Autre (précisez)
```

### 3. Contenu des logs
```
Copiez ici le contenu EXACT de :
- panosse_debug.log (sur votre Bureau)
- panosse_crash.log (sur votre Bureau, si existe)
```

### 4. Version Windows
```
Windows 10 ou Windows 11 ?
Version (ex: Windows 11 23H2) ?
```

---

## 🔧 RECOMPILATION PROPRE

Si vous avez un doute, voici comment recompiler PROPREMENT :

```powershell
# 1. Fermer TOUTES les instances de Panosse
taskkill /F /IM Panosse.exe 2>$null

# 2. Nettoyer COMPLÈTEMENT
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue

# 3. Restaurer les packages
dotnet restore

# 4. Compiler en Release
dotnet build -c Release

# 5. Tester
.\test-avec-logs.ps1
```

---

## 💡 SI TOUT ÉCHOUE

Si même après tout cela, Panosse ne se lance toujours pas :

1. **Testez la version v1.1.1** (si vous l'avez) pour confirmer que le problème est bien avec v2.0.0

2. **Créez une version Debug** :
```powershell
dotnet build -c Debug
.\bin\Debug\net8.0-windows\Panosse.exe
```

3. **Désactivez temporairement les nouvelles fonctionnalités v2.0.0** pour isoler le problème

---

## 🎉 DANS MES TESTS

✅ **L'application fonctionne parfaitement** avec le logging ajouté !

Toutes les étapes se terminent avec succès :
- Constructeur ✅
- InitializeComponent ✅
- MainWindow_Loaded ✅
- System Tray ✅
- HotKey ✅

**Donc le problème est spécifique à votre environnement.**

Avec les logs, nous allons identifier exactement ce qui ne va pas ! 🔍

---

**Lancez `.\test-avec-logs.ps1` et envoyez-moi le résultat !**

