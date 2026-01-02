# 🚀 Guide de Publication Panosse v2.0.0

## ✅ PRÉPARATION COMPLÈTE

### 📋 Checklist avant publication

- ✅ Version 2.0.0 définie dans `Panosse.csproj`
- ✅ Icônes `panosse.ico` et `panosse_sale.ico` configurées
- ✅ Configuration Single File activée
- ✅ Warnings de compilation corrigés (0 warning)
- ✅ Tests fonctionnels effectués

---

## 📦 MÉTHODE 1 : Script PowerShell (Recommandé)

### Utilisation du script automatisé

```powershell
.\publier-v2.0.ps1
```

Ce script effectue automatiquement :
1. ✅ Nettoyage des anciens builds
2. ✅ Restauration des dépendances
3. ✅ Compilation en mode Release
4. ✅ Publication Single File
5. ✅ Calcul du SHA256

---

## 💻 MÉTHODE 2 : Commande Manuelle

### Commande dotnet publish complète

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false
```

### Décomposition de la commande

```bash
dotnet publish \
  -c Release \                              # Configuration Release
  -r win-x64 \                              # Runtime Windows 64-bit
  --self-contained true \                   # Inclure le runtime .NET
  -p:PublishSingleFile=true \               # Un seul fichier .exe
  -p:IncludeNativeLibrariesForSelfExtract=true \  # Bibliothèques natives
  -p:EnableCompressionInSingleFile=true \   # Compression activée
  -p:PublishReadyToRun=true \               # AOT compilation
  -p:DebugType=None \                       # Pas de symboles debug
  -p:DebugSymbols=false                     # Pas de fichiers .pdb
```

---

## 📂 EMPLACEMENT DU FICHIER

Après la publication, l'exécutable se trouve à :

```
bin\Release\net8.0-windows\win-x64\publish\Panosse.exe
```

---

## 📊 VÉRIFICATION DE LA PUBLICATION

### Taille attendue
```
~70-75 Mo (avec runtime .NET 8.0 inclus)
```

### Vérification de l'intégrité

#### PowerShell
```powershell
Get-FileHash -Path "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe" -Algorithm SHA256
```

#### CMD
```cmd
certutil -hashfile "bin\Release\net8.0-windows\win-x64\publish\Panosse.exe" SHA256
```

---

## 🧪 TESTS POST-PUBLICATION

### Test 1 : Exécution de base
```powershell
cd bin\Release\net8.0-windows\win-x64\publish
.\Panosse.exe
```

**Vérifications** :
- ✅ Application démarre sans erreur
- ✅ Icône propre visible dans System Tray
- ✅ Interface s'affiche correctement
- ✅ Droits administrateur demandés

### Test 2 : Fonctionnalités v2.0
- ✅ Mémoire Sélective active (vérif après 30s)
- ✅ Raccourci Ctrl+Alt+P fonctionne
- ✅ Changement d'icône dynamique
- ✅ Menu "Pourquoi rouge?" apparaît si encombré
- ✅ Nettoyage remet l'icône propre

### Test 3 : Portabilité
1. Copier `Panosse.exe` dans un autre dossier
2. Lancer l'application
3. **Résultat attendu** : Fonctionne sans problème

---

## 🎯 CARACTÉRISTIQUES DE LA v2.0.0

### Nouveautés majeures

#### 🧠 Mémoire Sélective
- Surveillance automatique du dossier Téléchargements
- Vérification toutes les heures
- Seuils : > 5 Go OU fichiers > 200 Mo anciens > 30 jours

#### 🎨 Icônes Dynamiques
- **Propre** : `panosse.ico` (état normal)
- **Sale** : `panosse_sale.ico` (alerte encombrement)
- Changement automatique selon l'état

#### ⌨️ Raccourci Global
- **Ctrl+Alt+P** : Nettoyage en arrière-plan
- Notification Toast avec Mo libérés
- Son de réussite

#### 📋 System Tray Amélioré
- Menu contextuel enrichi
- "Pourquoi l'icône est rouge?" (si encombré)
- Tooltip dynamique avec infos

---

## 📦 CONFIGURATION DU .CSPROJ

### Version
```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
```

### Ressources embarquées
```xml
<ItemGroup>
  <!-- Ressources embarquées (incluses dans l'exécutable) -->
  <Resource Include="assets\panosse.png" />
  <Resource Include="assets\panosse.ico" />
  <Resource Include="assets\panosse_sale.ico" />
  <Resource Include="assets\panosse_sale.png" />
  
  <!-- Fichiers de contenu (fallback) -->
  <Content Include="assets\panosse.ico">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="assets\panosse_sale.ico">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Configuration Single File
```xml
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<PublishReadyToRun>true</PublishReadyToRun>
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
```

---

## 🔄 CRÉATION DE L'INSTALLER (OPTIONNEL)

### Avec Inno Setup

1. **Mettre à jour la version dans le script**
   ```
   Panosse-Setup.iss
   Ligne 6 : #define MyAppVersion "2.0.0"
   ```

2. **Générer l'installer**
   ```powershell
   .\creer-installateur.ps1
   ```

3. **Résultat**
   ```
   installer\Panosse-Setup-v2.0.0.exe
   ```

---

## 🌐 PUBLICATION GITHUB RELEASE

### Prérequis
```powershell
# Committer les changements
git add -A
git commit -m "v2.0.0 : Mémoire Sélective + Icônes dynamiques"
git push
```

### Créer le tag
```powershell
git tag v2.0.0
git push origin v2.0.0
```

### Créer la release sur GitHub

1. Aller sur : `https://github.com/barbarom84-ai/panosse/releases/new`
2. Tag : `v2.0.0`
3. Titre : `Panosse v2.0.0 - Mémoire Sélective`
4. Description :

```markdown
## 🧠 Nouveautés v2.0.0

### Mémoire Sélective
- 🔍 Surveillance automatique du dossier Téléchargements
- ⏰ Vérification toutes les heures
- ⚠️ Alerte visuelle si > 5 Go ou gros fichiers anciens

### Icônes Dynamiques
- 🟢 **Propre** : Serpillère normale
- 🔴 **Sale** : Serpillère avec alerte (encombrement détecté)
- 🔄 Changement automatique selon l'état

### Raccourci Global
- ⌨️ **Ctrl+Alt+P** : Nettoyage en arrière-plan
- 💬 Notification avec Mo libérés
- 🔊 Son de réussite

### Améliorations
- 📋 Menu contextuel enrichi ("Pourquoi rouge?")
- 🔄 Reset automatique icône après nettoyage
- 🎨 Interface System Tray améliorée

## 📥 Installation

**Portable** : Téléchargez `Panosse.exe`
**Installateur** : Téléchargez `Panosse-Setup-v2.0.0.exe`

## 🔐 SHA256

Panosse.exe : [VOTRE_SHA256_ICI]

## ⚙️ Prérequis
- Windows 10/11 (64 bits)
- Droits administrateur
```

5. Uploader les fichiers :
   - `Panosse.exe` (portable)
   - `Panosse-Setup-v2.0.0.exe` (installateur)

---

## 📝 NOTES DE VERSION COMPLÈTES

### v2.0.0 (2025-01-02)

#### Ajouts
- ✨ **Mémoire Sélective** : Surveillance intelligente Downloads
- ✨ **Icônes dynamiques** : panosse.ico / panosse_sale.ico
- ✨ **Raccourci Ctrl+Alt+P** : Nettoyage en arrière-plan
- ✨ **Menu contextuel enrichi** : "Pourquoi rouge?"
- ✨ **Reset automatique** : Icône propre après nettoyage

#### Améliorations
- 🎨 Interface System Tray plus informative
- 🔔 Notifications enrichies avec détails
- ⚡ Surveillance asynchrone (0% CPU)
- 🛡️ Warnings nullabilité corrigés

#### Corrections
- 🐛 Chargement icônes depuis ressources embarquées
- 🐛 Vérifications null pour notifyIcon
- 🐛 Warning WFAC010 (DPI) résolu

---

## 🎊 RÉSUMÉ

### Commande de publication
```powershell
.\publier-v2.0.ps1
```

### Ou manuellement
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishReadyToRun=true -p:DebugType=None -p:DebugSymbols=false
```

### Fichier généré
```
bin\Release\net8.0-windows\win-x64\publish\Panosse.exe (~70-75 Mo)
```

### Prêt pour
- ✅ Distribution directe (portable)
- ✅ Création installer Inno Setup
- ✅ Upload GitHub Release
- ✅ Partage utilisateurs

---

**Panosse v2.0.0 prêt à nettoyer ! 🧹✨**

