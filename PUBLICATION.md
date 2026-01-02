# 📦 Guide de Publication - Panosse

## 🎯 Objectif
Compiler Panosse en un **seul fichier .exe** autonome, prêt à être distribué.

---

## 🚀 Méthode 1 : Script PowerShell (Recommandé)

### Utilisation simple :

```powershell
.\publier.ps1
```

Le fichier `Panosse.exe` sera généré dans le dossier `.\publish\`

---

## 🛠️ Méthode 2 : Commande dotnet manuelle

### Commande complète :

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish
```

### Explication des paramètres :

| Paramètre | Description |
|-----------|-------------|
| `-c Release` | Compile en mode Release (optimisé) |
| `-r win-x64` | Cible Windows 64 bits |
| `--self-contained true` | Inclut le runtime .NET (pas besoin d'installation) |
| `-p:PublishSingleFile=true` | Génère un seul fichier .exe |
| `-p:IncludeNativeLibrariesForSelfExtract=true` | Inclut les DLL natives |
| `-p:EnableCompressionInSingleFile=true` | Compresse le fichier (taille réduite) |
| `-o .\publish` | Dossier de sortie |

---

## 📋 Versions alternatives

### Version 32 bits (pour compatibilité maximale) :

```powershell
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish-x86
```

### Version sans runtime inclus (plus petite, nécessite .NET 8) :

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\publish-framework
```

---

## 📊 Comparaison des tailles

| Type | Taille approximative | Avantages | Inconvénients |
|------|---------------------|-----------|---------------|
| **Self-contained (recommandé)** | ~70-90 Mo | Fonctionne partout, aucune dépendance | Fichier plus gros |
| **Framework-dependent** | ~1-5 Mo | Fichier très léger | Nécessite .NET 8 installé |

---

## ✅ Vérification après compilation

### 1. Vérifier la taille du fichier :

```powershell
Get-Item .\publish\Panosse.exe | Select-Object Name, Length, @{Name="SizeMB";Expression={[math]::Round($_.Length/1MB,2)}}
```

### 2. Tester l'application :

```powershell
Start-Process ".\publish\Panosse.exe" -Verb RunAs
```

### 3. Vérifier les propriétés :

- Clic droit sur `Panosse.exe` → Propriétés
- Onglet **Détails** : Vérifier la version, description, etc.

---

## 📦 Distribution

### Le fichier généré peut être distribué de plusieurs façons :

1. **Copie directe** : Partagez simplement le fichier `Panosse.exe`
2. **Archive ZIP** : Compressez avec les instructions d'utilisation
3. **Installateur** : Utilisez un créateur d'installateur (Inno Setup, WiX, etc.)

### ⚠️ Important :

- L'application nécessite des **droits administrateur** pour fonctionner
- Windows Defender peut analyser le fichier au premier lancement (normal)
- Certains antivirus peuvent signaler un "unknown publisher" (normal, signez le .exe pour éviter cela)

---

## 🔐 Signature de code (optionnel, pour production)

Pour éviter les avertissements Windows, vous pouvez signer le fichier :

```powershell
signtool sign /f "votre-certificat.pfx" /p "mot-de-passe" /t http://timestamp.digicert.com ".\publish\Panosse.exe"
```

---

## 🧪 Tests recommandés avant distribution

- [ ] Lancer sur une machine propre (sans Visual Studio/SDK)
- [ ] Vérifier que tous les nettoyages fonctionnent
- [ ] Tester l'icône et l'interface
- [ ] Vérifier la demande de droits administrateur (UAC)
- [ ] Tester sur Windows 10 et Windows 11

---

## 📝 Notes

- Le fichier `.csproj` a déjà été configuré pour la publication single-file
- Le runtime .NET 8 est inclus dans le fichier généré
- La compression est activée pour réduire la taille
- L'optimisation ReadyToRun est activée pour un démarrage plus rapide

---

## 🆘 Dépannage

### Erreur "runtime not found" :

```powershell
dotnet --list-runtimes
```

Si .NET 8 n'est pas listé, installez-le depuis : https://dotnet.microsoft.com/download

### Le fichier est trop gros :

- Désactivez `SelfContained` (mais .NET 8 sera requis sur la machine cible)
- Utilisez `PublishTrimmed=true` (peut casser certaines fonctionnalités)

### L'antivirus bloque l'exécution :

- C'est normal pour un nouvel exécutable non signé
- Ajoutez une exception dans l'antivirus
- Ou signez le fichier avec un certificat de code

---

**🧹 Bonne distribution de Panosse ! ✨**

