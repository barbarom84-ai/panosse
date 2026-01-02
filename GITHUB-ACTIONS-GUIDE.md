# 🤖 GitHub Actions - Guide d'utilisation

## ✅ Workflow automatique créé !

Le fichier `.github/workflows/build.yml` a été créé. Il automatise :
- ✅ Compilation en mode Release
- ✅ Création d'un Single File .exe
- ✅ Calcul du checksum SHA256
- ✅ Création automatique d'une GitHub Release
- ✅ Upload de l'exécutable en tant qu'Asset

---

## 🚀 Comment utiliser ce workflow

### 1️⃣ Pousser le workflow vers GitHub

```powershell
cd "C:\Users\marco\Cursor Workplace\panosse"

# Ajouter le nouveau fichier
& "C:\Program Files\Git\bin\git.exe" add .github/workflows/build.yml

# Commiter
& "C:\Program Files\Git\bin\git.exe" commit -m "Ajout du workflow GitHub Actions pour releases automatiques"

# Pousser
& "C:\Program Files\Git\bin\git.exe" push
```

### 2️⃣ Créer un tag de version

Quand vous êtes prêt à publier une nouvelle version :

```powershell
# Créer un tag (ex: v1.0.0)
& "C:\Program Files\Git\bin\git.exe" tag v1.0.0

# Pousser le tag vers GitHub
& "C:\Program Files\Git\bin\git.exe" push origin v1.0.0
```

**🎯 Le workflow se déclenchera automatiquement !**

### 3️⃣ Suivre la progression

1. Allez sur votre dépôt GitHub
2. Onglet **"Actions"**
3. Vous verrez le workflow en cours d'exécution
4. Durée estimée : **~5 minutes**

### 4️⃣ Récupérer la release

Une fois terminé :
1. Onglet **"Releases"**
2. Votre nouvelle release sera créée automatiquement
3. L'exécutable `Panosse-v1.0.0.exe` sera disponible en téléchargement
4. Le checksum SHA256 sera affiché dans la description

---

## 📋 Exemple complet de workflow

### Scénario : Publier la version 1.0.1

```powershell
cd "C:\Users\marco\Cursor Workplace\panosse"

# 1. Faire vos modifications...
# (ex: corriger un bug, ajouter une fonctionnalité)

# 2. Vérifier les changements
& "C:\Program Files\Git\bin\git.exe" status

# 3. Ajouter les modifications
& "C:\Program Files\Git\bin\git.exe" add .

# 4. Commiter
& "C:\Program Files\Git\bin\git.exe" commit -m "Fix: Correction du bug de progression"

# 5. Pousser
& "C:\Program Files\Git\bin\git.exe" push

# 6. Créer le tag
& "C:\Program Files\Git\bin\git.exe" tag v1.0.1 -m "Version 1.0.1 - Correction de bugs"

# 7. Pousser le tag (DÉCLENCHE LE WORKFLOW)
& "C:\Program Files\Git\bin\git.exe" push origin v1.0.1
```

**🤖 GitHub Actions fera tout le reste automatiquement !**

---

## 🎯 Ce que le workflow fait

### Étape 1 : Checkout du code
Récupère votre code source depuis GitHub

### Étape 2 : Installation de .NET 8.0
Configure l'environnement de build

### Étape 3 : Restauration des dépendances
`dotnet restore`

### Étape 4 : Compilation Release
```powershell
dotnet publish Panosse.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:PublishReadyToRun=true `
  -o publish
```

### Étape 5 : Renommage
`Panosse.exe` → `Panosse-v1.0.0.exe`

### Étape 6 : Calcul du checksum
Génère le SHA256 pour vérification de l'intégrité

### Étape 7 : Création de la Release
Crée automatiquement une release GitHub avec description

### Étape 8 : Upload de l'exécutable
Ajoute l'exécutable en tant qu'Asset téléchargeable

### Étape 9 : Résumé
Affiche les informations de build dans les logs

---

## 🏷️ Conventions de nommage des tags

### Format recommandé : Semantic Versioning

```
v[MAJOR].[MINOR].[PATCH]
```

**Exemples** :
- `v1.0.0` - Version initiale
- `v1.0.1` - Correction de bugs mineurs
- `v1.1.0` - Ajout de nouvelles fonctionnalités
- `v2.0.0` - Changements majeurs / incompatibilités

### Règles
- ✅ **v1.0.0** - Valide
- ✅ **v1.2.3** - Valide
- ✅ **v2.0.0-beta** - Valide (pré-release)
- ❌ **1.0.0** - Invalide (manque le 'v')
- ❌ **version-1** - Invalide (ne commence pas par 'v*')

---

## 📝 Commandes Git utiles pour les tags

### Créer un tag annoté (recommandé)
```powershell
& "C:\Program Files\Git\bin\git.exe" tag v1.0.0 -m "Version 1.0.0 - Release initiale"
```

### Créer un tag léger
```powershell
& "C:\Program Files\Git\bin\git.exe" tag v1.0.0
```

### Lister tous les tags
```powershell
& "C:\Program Files\Git\bin\git.exe" tag
```

### Voir les détails d'un tag
```powershell
& "C:\Program Files\Git\bin\git.exe" show v1.0.0
```

### Supprimer un tag localement
```powershell
& "C:\Program Files\Git\bin\git.exe" tag -d v1.0.0
```

### Supprimer un tag sur GitHub
```powershell
& "C:\Program Files\Git\bin\git.exe" push origin --delete v1.0.0
```

### Pousser tous les tags
```powershell
& "C:\Program Files\Git\bin\git.exe" push --tags
```

---

## 🔍 Vérifier le workflow

### Voir les workflows disponibles
Sur GitHub : `https://github.com/barbarom84-ai/panosse/actions`

### Voir l'exécution en cours
1. Onglet "Actions"
2. Cliquez sur le workflow en cours
3. Voyez les logs en temps réel

### Voir les releases créées
Sur GitHub : `https://github.com/barbarom84-ai/panosse/releases`

---

## 🐛 Dépannage

### Le workflow ne se déclenche pas

**Vérifiez** :
1. Le fichier est bien dans `.github/workflows/build.yml`
2. Vous avez poussé le fichier : `git push`
3. Le tag commence bien par 'v' : `v1.0.0` ✅ vs `1.0.0` ❌
4. Vous avez poussé le tag : `git push origin v1.0.0`

### Erreur de compilation

**Causes possibles** :
- Erreur dans le code C#
- Dépendance manquante
- Problème dans le .csproj

**Solution** :
1. Testez localement d'abord : `dotnet build -c Release`
2. Corrigez les erreurs
3. Recommitez et retaguez

### Erreur "release already exists"

Si vous avez déjà une release avec ce tag :
1. Supprimez la release sur GitHub
2. Supprimez le tag : `git push origin --delete v1.0.0`
3. Recréez le tag : `git tag v1.0.0`
4. Repoussez : `git push origin v1.0.0`

---

## 🎨 Personnaliser la description de la release

Éditez `.github/workflows/build.yml`, section `body:` :

```yaml
body: |
  ## 🧹 Panosse ${{ github.ref_name }}
  
  **Votre description personnalisée ici !**
  
  ### 🆕 Nouveautés
  
  - Nouvelle fonctionnalité X
  - Amélioration Y
  - Correction du bug Z
```

---

## 📊 Statistiques

**Temps de build** : ~5 minutes
**Taille de l'exécutable** : ~60-80 MB (self-contained)
**Plateforme** : Windows 64-bit uniquement

---

## 🚀 Prochaines améliorations possibles

### 1. Build multi-plateforme
Ajouter des jobs pour Linux et macOS (si applicable)

### 2. Tests automatiques
Ajouter des tests unitaires avant la compilation

### 3. Notifications
Envoyer un email ou une notification Discord à chaque release

### 4. Changelog automatique
Générer automatiquement un changelog basé sur les commits

### 5. Code signing
Signer l'exécutable avec un certificat de code

---

## ✅ Checklist pour votre première release automatique

- [ ] Pousser le workflow vers GitHub
- [ ] Vérifier que le workflow apparaît dans l'onglet Actions
- [ ] Créer le tag v1.0.0
- [ ] Pousser le tag : `git push origin v1.0.0`
- [ ] Attendre ~5 minutes
- [ ] Vérifier la release dans l'onglet Releases
- [ ] Télécharger et tester l'exécutable

---

## 🎉 Félicitations !

Vous avez maintenant un système de **Continuous Deployment** professionnel !

À chaque nouveau tag, GitHub Actions :
- ✅ Compile automatiquement
- ✅ Crée une release
- ✅ Publie l'exécutable
- ✅ Calcule les checksums
- ✅ Génère la documentation

**Plus besoin de compiler manuellement ! 🚀**

---

## 📚 Ressources

- **GitHub Actions** : https://docs.github.com/en/actions
- **Semantic Versioning** : https://semver.org/
- **dotnet publish** : https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish

---

**🤖 Workflow créé et prêt à l'emploi !**

