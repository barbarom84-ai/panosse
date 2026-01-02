# 📦 Guide Inno Setup - Panosse

## 🎯 Créer un installateur professionnel pour Panosse

---

## 📋 Prérequis

### 1. Installer Inno Setup

Téléchargez et installez **Inno Setup** (gratuit) :
- 🔗 Site officiel : https://jrsoftware.org/isinfo.php
- 📥 Télécharger : **Inno Setup 6.x** (version recommandée)
- ⚙️ Installation : Suivez l'assistant (installation standard)

### 2. Compiler l'application

Avant de créer l'installateur, compilez Panosse :

```powershell
.\publier.ps1
```

Ou manuellement :

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish
```

---

## 🚀 Créer l'installateur

### Méthode 1 : Via l'interface Inno Setup

1. **Ouvrir Inno Setup Compiler**
2. **Fichier → Ouvrir** → Sélectionner `Panosse-Setup.iss`
3. **Build → Compile** (ou appuyer sur `Ctrl+F9`)
4. L'installateur sera créé dans le dossier `installer\`

### Méthode 2 : En ligne de commande

```powershell
# Compiler le script .iss
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "Panosse-Setup.iss"
```

### Méthode 3 : Script PowerShell automatisé

Créez un fichier `creer-installateur.ps1` :

```powershell
# Compiler l'application
Write-Host "1. Compilation de Panosse..." -ForegroundColor Yellow
.\publier.ps1

# Compiler l'installateur
Write-Host "`n2. Création de l'installateur..." -ForegroundColor Yellow
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "Panosse-Setup.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Installateur créé avec succès !" -ForegroundColor Green
    Write-Host "Fichier : .\installer\Panosse-Setup-v1.0.0.exe" -ForegroundColor Cyan
}
```

---

## 📊 Résultat

Après compilation, vous obtiendrez :

```
📁 installer\
  └─ Panosse-Setup-v1.0.0.exe (~75-80 Mo)
```

---

## ✨ Fonctionnalités de l'installateur

### 🎨 Interface

- ✅ Interface moderne et professionnelle
- ✅ Assistant d'installation en français et anglais
- ✅ Icône personnalisée (panosse.ico)
- ✅ Messages personnalisés

### 🛠️ Installation

- ✅ **Dossier par défaut** : `C:\Program Files\Panosse\`
- ✅ **Droits administrateur** : Demandés automatiquement
- ✅ **Compression** : LZMA2 maximum (fichier plus petit)
- ✅ **Architecture** : Windows 64 bits uniquement

### 🔗 Raccourcis créés

- ✅ **Menu Démarrer** : `Panosse` + `Désinstaller Panosse`
- ✅ **Bureau** : Icône `Panosse` (optionnel, coché par défaut)
- ✅ **Barre de lancement** : Icône rapide (optionnel, non coché)

### 📄 Fichiers inclus

- ✅ `Panosse.exe` (application principale)
- ✅ `panosse.ico` (icône)
- ✅ `panosse.png` (image)
- ✅ `LisezMoi.txt` (README converti)
- ✅ `Guide-Publication.txt` (guide de publication)

### 🗑️ Désinstallation

- ✅ Désinstalleur propre dans "Programmes et fonctionnalités"
- ✅ Suppression de tous les fichiers et raccourcis
- ✅ Nettoyage complet du dossier d'installation

---

## 🔧 Personnalisation du script

### Modifier la version

Dans `Panosse-Setup.iss`, ligne 7 :

```pascal
#define MyAppVersion "1.0.0"
```

Changez en `1.1.0`, `2.0.0`, etc.

### Modifier le dossier source

Si votre dossier de publication change, ligne 49 :

```pascal
Source: "publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
```

Remplacez `publish\` par votre chemin.

### Ajouter une licence

1. Créez un fichier `LICENSE.txt` à la racine
2. Dans le script, ligne 38 :

```pascal
LicenseFile=LICENSE.txt
```

### Ajouter un fichier "Lisez-moi" avant installation

1. Créez `AVANT-INSTALLATION.txt`
2. Ligne 39 :

```pascal
InfoBeforeFile=AVANT-INSTALLATION.txt
```

---

## 📝 Structure du script .iss

### Sections principales

| Section | Description |
|---------|-------------|
| `[Setup]` | Configuration générale de l'installateur |
| `[Languages]` | Langues disponibles (français, anglais) |
| `[Tasks]` | Tâches optionnelles (icônes bureau, etc.) |
| `[Files]` | Fichiers à installer |
| `[Icons]` | Raccourcis à créer |
| `[Run]` | Actions après installation |
| `[Code]` | Code Pascal personnalisé |
| `[CustomMessages]` | Messages personnalisés |

---

## 🎯 Bonnes pratiques

### Avant de distribuer

- [ ] Tester l'installateur sur une machine propre
- [ ] Vérifier que tous les fichiers sont inclus
- [ ] Tester l'installation ET la désinstallation
- [ ] Vérifier les raccourcis (bureau, menu)
- [ ] Tester sur Windows 10 et Windows 11

### Distribution

1. **Nom du fichier** : `Panosse-Setup-v1.0.0.exe` (clair et versionné)
2. **Hébergement** : GitHub Releases, site web, etc.
3. **Checksum** : Fournir un hash SHA256 pour vérifier l'intégrité

Générer le hash :

```powershell
Get-FileHash .\installer\Panosse-Setup-v1.0.0.exe -Algorithm SHA256 | Format-List
```

---

## 🔐 Signature de l'installateur (optionnel)

Pour éviter les avertissements "Éditeur inconnu" :

```powershell
signtool sign /f "certificat.pfx" /p "mot-de-passe" /t http://timestamp.digicert.com ".\installer\Panosse-Setup-v1.0.0.exe"
```

Nécessite un **certificat de signature de code** (~150-300€/an).

---

## 🆘 Dépannage

### Erreur : "Can't find Inno Setup compiler"

Vérifiez le chemin d'installation :

```powershell
Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

Si faux, ajustez le chemin dans votre commande.

### Erreur : "Source file not found"

Vérifiez que le dossier `publish\` existe et contient `Panosse.exe` :

```powershell
Test-Path .\publish\Panosse.exe
```

Si faux, lancez d'abord `.\publier.ps1`.

### L'installateur est trop gros

Normal ! Il contient :
- Application (74 Mo)
- Ressources
- Runtime .NET

Vous ne pouvez pas vraiment réduire la taille sans compromettre la fonctionnalité.

### Windows Defender bloque l'installateur

C'est normal pour un nouvel exécutable. Solutions :
1. Signer l'installateur avec un certificat
2. Ajouter une exception dans Windows Defender
3. Distribuer le hash SHA256 pour que les utilisateurs vérifient

---

## 📦 Versions alternatives

### Installateur silencieux (pour déploiement en masse)

```powershell
.\Panosse-Setup-v1.0.0.exe /VERYSILENT /NORESTART /SUPPRESSMSGBOXES
```

### Installation personnalisée

```powershell
# Installer dans un dossier spécifique
.\Panosse-Setup-v1.0.0.exe /DIR="D:\Apps\Panosse"

# Installer sans icône bureau
.\Panosse-Setup-v1.0.0.exe /TASKS="!desktopicon"
```

---

## 📈 Comparaison : EXE simple vs Installateur

| Aspect | EXE simple | Installateur Inno |
|--------|------------|-------------------|
| **Taille** | 74 Mo | 75-80 Mo |
| **Professionnalisme** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Installation** | Copie manuelle | Assistant guidé |
| **Raccourcis** | Manuel | Automatique |
| **Désinstallation** | Manuel | Propre |
| **Distribution** | Simple | Professionnelle |

**Recommandation** : Utilisez l'installateur pour une distribution professionnelle !

---

## 🎨 Améliorations possibles

### Ajouter des images personnalisées

Remplacez les images par défaut dans le script :

```pascal
WizardImageFile=mes-images\grand-logo.bmp      ; 164x314 pixels
WizardSmallImageFile=mes-images\petit-logo.bmp ; 55x58 pixels
```

### Créer un installateur multi-langues

Ajoutez d'autres langues dans `[Languages]` :

```pascal
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
```

### Vérifier les prérequis

Ajoutez du code Pascal pour vérifier Windows 10+ :

```pascal
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  GetWindowsVersionEx(Version);
  if Version.Major < 10 then
  begin
    MsgBox('Panosse nécessite Windows 10 ou supérieur.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;
```

---

## 📚 Ressources

- 📖 Documentation Inno Setup : https://jrsoftware.org/ishelp/
- 💬 Forum Inno Setup : https://groups.google.com/g/innosetup
- 📘 Exemples : `C:\Program Files (x86)\Inno Setup 6\Examples\`

---

**🧹 Votre installateur professionnel est prêt ! ✨**

Testez-le et distribuez Panosse en toute confiance ! 🎉

