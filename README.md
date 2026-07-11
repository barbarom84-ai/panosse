# Panosse - Outil de Nettoyage Minimaliste 🧹

Application WPF moderne pour nettoyer rapidement votre système Windows en un seul clic.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## ✨ Fonctionnalités

### 🎨 Interface Moderne
- **Design minimaliste** : Format portrait (400x550) sans bordures Windows
- **Dégradé élégant** : Fond gris clair vers blanc
- **Bouton circulaire bleu** avec icône de nettoyage intégrée
- **Animation de pulsation** pendant le nettoyage
- **Bouton Quitter** stylisé en haut à droite
- **Fenêtre déplaçable** : Cliquez n'importe où pour déplacer

### 🧹 Nettoyage Complet (8 étapes)
- ✅ **Corbeille** : Vidage complet via API Shell32
- ✅ **Fichiers temporaires** : 
  - Dossier utilisateur (%TEMP%)
  - Dossier Windows (C:\Windows\Temp)
- ✅ **Cache Google Chrome** : Cache, Code Cache, GPU Cache
- ✅ **Cache Microsoft Edge** : Cache, Code Cache, GPU Cache
- ✅ **Registre Windows** : Historique commandes et documents récents
- ✅ **Téléchargements anciens** : Fichiers .exe et .msi de plus de 14 jours
- ✅ **Logs Windows** : Fichiers journaux de plus de 7 jours
- ✅ **Cache miniatures** : Thumbnails et icônes Windows

### 🛡️ Sécurité & Fiabilité
- **Droits administrateur** : Demandés automatiquement au lancement
- **Gestion intelligente** : Ignore les fichiers/dossiers verrouillés
- **Détection navigateurs** : Alerte si Chrome/Edge sont ouverts
- **Asynchrone** : Interface toujours fluide, jamais figée

### 📊 Retour Utilisateur
- **Barre de progression** moderne avec 8 étapes
- **Liste détaillée des tâches** avec animation de fondu
- **Messages colorés** :
  - 🟢 Vert : Nettoyage réussi (barre + message)
  - 🟠 Orange : Avertissement navigateurs ouverts
  - 🔵 Bleu : Opération en cours
- **Calcul précis** : Affiche l'espace libéré par catégorie
- **Animation de célébration** : Rebond du message de succès

### 🔄 Mises à jour automatiques
- **Vérification au démarrage** : Connexion à l'API GitHub pour vérifier les nouvelles versions
- **Notification discrète** : Barre verte en haut avec animation slide-in
- **🆕 Installation automatique en 1 clic** : 
  - Télécharge la nouvelle version directement
  - Remplace l'ancien exécutable automatiquement
  - Redémarre l'application avec la nouvelle version
  - Aucune manipulation manuelle requise
- **Sauvegarde automatique** : L'ancienne version est sauvegardée (.exe.old)
- **Rollback automatique** : Restaure l'ancienne version en cas d'erreur
- **Comparaison intelligente** : Utilise Semantic Versioning (MAJOR.MINOR.PATCH)
- **Gestion des erreurs** : Fonctionne silencieusement, aucune alerte si offline
- **Fallback manuel** : Ouvre la page GitHub si le téléchargement automatique échoue

## 🚀 Installation & Utilisation

### Option 1 : Installateur (Recommandé)

Téléchargez et exécutez `Panosse-Setup-v1.0.0.exe`

L'installateur créera automatiquement :
- Installation dans `C:\Program Files\Panosse\`
- Raccourci sur le bureau
- Raccourci dans le menu Démarrer
- Désinstalleur dans "Programmes et fonctionnalités"

### Option 2 : Fichier EXE portable

Téléchargez simplement `Panosse.exe` (74 Mo) et lancez-le.
- Aucune installation requise
- Fonctionne partout (runtime .NET inclus)

### Prérequis
- Windows 10/11 (64 bits)
- Droits administrateur

---

## 🛠️ Pour les développeurs

### Compilation du projet

```powershell
# Clone du dépôt
git clone https://github.com/barbarom84-ai/panosse.git
cd panosse

# Compilation en mode Debug
dotnet build

# Lancement
Start-Process "bin\Debug\net8.0-windows\Panosse.exe" -Verb RunAs
```

### Créer une version distribuable (Single File)

```powershell
# Méthode automatique (recommandé)
.\publier.ps1

# Méthode manuelle
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish
```

Le fichier `Panosse.exe` sera dans `.\publish\`

### Créer un installateur

```powershell
# Prérequis : Installer Inno Setup
# https://jrsoftware.org/isinfo.php

# Créer l'installateur automatiquement
.\creer-installateur.ps1
```

L'installateur `Panosse-Setup-v1.0.0.exe` sera dans `.\installer\`

Consultez `INNO-SETUP-GUIDE.md` pour plus de détails.

### Checklist de release mensuelle

Consultez `docs/release/monthly-release-checklist.md` avant chaque publication.
Pour la migration WinUI, utilisez aussi `docs/release/winui-rc-checklist.md`.
Les release notes WinUI beta sont disponibles dans `docs/release/winui-v3-beta-release-notes.md`.

### Build de la solution multi-projets (WPF + WinUI)

```powershell
# Build global
dotnet build Panosse.slnx

# Build release pour validation RC WinUI
dotnet build Panosse.slnx -c Release
```

### Roadmap 2026 (documents d'exécution)

- Spécifications Q1 : `docs/roadmap/2026/q1-feature-specs.md`
- Cibles d'extraction architecture : `docs/roadmap/2026/architecture-extraction-targets.md`
- Compteurs telemetry locaux : `docs/roadmap/2026/telemetry-counters.md`

## 📋 Utilisation

1. **Lancez l'application** → La fenêtre UAC demande les droits admin
2. **Vérifiez l'alerte** → Si Chrome/Edge sont ouverts, fermez-les
3. **Cliquez sur le bouton bleu** → Le nettoyage démarre
4. **Observez l'animation** → Le bouton pulse pendant l'opération
5. **Résultat** → Message vert avec l'espace libéré

## 🎯 Spécifications Techniques

### Architecture
- **Pattern** : Code-behind WPF
- **Threading** : Task.Run pour opérations de fond
- **API natives** : shell32.dll (SHEmptyRecycleBin)
- **Animations** : Storyboard WPF avec KeyFrames

### Sécurité
- **Manifeste UAC** : requireAdministrator
- **Try-catch** : Sur toutes les opérations fichiers
- **Nettoyage récursif** : Avec gestion d'erreurs par fichier

### Performance
- **Asynchrone** : Interface jamais bloquée
- **Calcul en temps réel** : Taille des fichiers avant suppression
- **Optimisé** : Parcours récursif efficace

## 📝 Checklist d'Implémentation

✅ **1. Design XAML**
- [x] Format portrait 400x550
- [x] WindowStyle="None" (sans bordures)
- [x] Dégradé gris → blanc
- [x] Bouton circulaire bleu avec icône
- [x] Barre de progression discrète
- [x] Label de statut

✅ **2. Logique de Nettoyage**
- [x] Méthode asynchrone
- [x] SHEmptyRecycleBin (API Shell32)
- [x] Nettoyage dossiers Temp
- [x] Cache Chrome
- [x] Cache Edge
- [x] Try-catch sur fichiers verrouillés
- [x] Calcul espace libéré

✅ **3. Élévation Privilèges**
- [x] app.manifest créé
- [x] requireAdministrator configuré
- [x] Lié au projet

✅ **4. Expérience Utilisateur**
- [x] Texte "Nettoyage en cours..."
- [x] Animation de pulsation
- [x] Message vert de succès
- [x] Format "Votre PC est tout propre ! X Mo libérés"
- [x] Bouton Quitter stylisé

✅ **5. Optimisations**
- [x] Task.Run (tâche de fond)
- [x] Interface non figée
- [x] Vérification navigateurs ouverts
- [x] Message d'avertissement

## 🎨 Palette de Couleurs

- **Fond** : Dégradé #F5F5F5 → #FFFFFF
- **Bouton principal** : #2196F3 (Bleu Material)
- **Bouton survol** : #42A5F5 (Bleu clair)
- **Bouton pressé** : #1976D2 (Bleu foncé)
- **Succès** : #4CAF50 (Vert)
- **Avertissement** : #FF9800 (Orange)
- **Texte** : #424242 (Gris foncé)

## 📦 Structure du Projet

```
panosse/
├── App.xaml                  # Configuration application
├── App.xaml.cs               # Code-behind application
├── MainWindow.xaml           # Interface utilisateur
├── MainWindow.xaml.cs        # Logique métier (759 lignes)
├── app.manifest              # Manifeste UAC
├── Panosse.csproj            # Configuration projet
├── assets/                   # Ressources
│   ├── panosse.ico          # Icône de l'application
│   └── panosse.png          # Image du bouton
├── Panosse-Setup.iss         # Script Inno Setup
├── publier.ps1               # Script de publication
├── creer-installateur.ps1    # Script création installateur
├── README.md                 # Documentation principale
├── PUBLICATION.md            # Guide de publication
└── INNO-SETUP-GUIDE.md       # Guide Inno Setup
```

## 🐛 Gestion des Erreurs

L'application gère intelligemment :
- **Fichiers en cours d'utilisation** : Ignorés sans erreur
- **Dossiers protégés** : Sautés automatiquement
- **Navigateurs ouverts** : Avertissement mais continue
- **Permissions insuffisantes** : Demande UAC au lancement

## 📄 Licence

Libre d'utilisation et de modification.

## 👨‍💻 Développement

Créé avec ❤️ en C# WPF / .NET 8.0

---

**Note** : Cette application nécessite les droits administrateur pour nettoyer efficacement votre système. Toutes les opérations sont sécurisées et ne suppriment que des fichiers temporaires.
