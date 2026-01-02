# 📝 Guide Rapide : Mettre à jour la version de Panosse

## ✅ État actuel de votre `.csproj`

Votre fichier `Panosse.csproj` est **déjà configuré** avec les balises de version :

```xml
<!-- Informations de version -->
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

**Tout est prêt !** 🎉

---

## 🚀 Comment passer à la version 1.1 (Méthode automatique)

### Option 1 : Utiliser le script PowerShell (RECOMMANDÉ)

**Le plus simple !** Nous avons créé un script qui fait tout automatiquement :

```powershell
.\bump-version.ps1 -NewVersion "1.1.0"
```

**Le script va** :
1. ✅ Mettre à jour automatiquement le `.csproj`
2. ✅ Vous proposer de commiter
3. ✅ Vous proposer de pusher
4. ✅ Vous proposer de créer la release immédiatement

**C'est fait en 10 secondes !** ⚡

---

## ✏️ Comment passer à la version 1.1 (Méthode manuelle)

Si vous préférez faire manuellement :

### Étape 1 : Ouvrir `Panosse.csproj`

Ouvrez le fichier avec votre éditeur (Cursor, Notepad++, VS Code, etc.)

### Étape 2 : Modifier les 3 lignes de version

**Cherchez** (lignes 20-22) :
```xml
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

**Changez en** :
```xml
<Version>1.1.0</Version>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

**⚠️ Important** : Changez les 3 lignes pour qu'elles soient cohérentes !

### Étape 3 : Sauvegarder

Appuyez sur `Ctrl+S` pour sauvegarder.

### Étape 4 : Vérifier

Recompilez pour vérifier :
```powershell
dotnet build
```

### Étape 5 : Commiter

```powershell
git add Panosse.csproj
git commit -m "Bump version to 1.1.0"
git push
```

### Étape 6 : Créer la release

```powershell
.\release-simple.ps1 -Version "1.1.0"
```

---

## 📊 Explication des 3 balises

### 1. `<Version>1.0.0</Version>`

**Utilisation** :
- Version du produit
- Affichée dans "À propos"
- Utilisée pour la vérification de mise à jour
- Format : `MAJOR.MINOR.PATCH`

**Exemples** :
- `1.0.0` → Version initiale
- `1.0.1` → Correction de bugs
- `1.1.0` → Nouvelles fonctionnalités
- `2.0.0` → Changements majeurs

### 2. `<AssemblyVersion>1.0.0.0</AssemblyVersion>`

**Utilisation** :
- Version de l'assembly .NET
- Utilisée pour la compatibilité binaire
- Format : `MAJOR.MINOR.BUILD.REVISION`

**Règle** : Ajoutez `.0` à la version du produit.

**Exemples** :
- `1.0.0` → `1.0.0.0`
- `1.1.0` → `1.1.0.0`
- `2.0.0` → `2.0.0.0`

### 3. `<FileVersion>1.0.0.0</FileVersion>`

**Utilisation** :
- Version du fichier Windows
- Affichée dans Propriétés du fichier (clic droit sur `.exe`)
- Format : `MAJOR.MINOR.BUILD.REVISION`

**Règle** : Identique à `AssemblyVersion`.

---

## 🎯 Exemples concrets de mise à jour

### Scénario 1 : Correction de bugs mineurs (v1.0.1)

**Modifications** :
```xml
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

**Quand l'utiliser** :
- Correction d'un bug
- Amélioration de performance
- Pas de nouvelles fonctionnalités

### Scénario 2 : Nouvelles fonctionnalités (v1.1.0)

**Modifications** :
```xml
<Version>1.1.0</Version>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

**Quand l'utiliser** :
- Ajout de fonctionnalités
- Améliorations majeures
- Nouvelle interface

### Scénario 3 : Changements majeurs (v2.0.0)

**Modifications** :
```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
```

**Quand l'utiliser** :
- Refonte complète
- Incompatibilités avec versions précédentes
- Changements architecturaux majeurs

---

## ⚡ Workflow complet pour v1.1

### Avec le script (RAPIDE)

```powershell
# Tout en une commande !
.\bump-version.ps1 -NewVersion "1.1.0"

# Répondez "o" aux questions :
# - Commiter ? o
# - Pusher ? o
# - Créer la release ? o

# Attendez 5 minutes → Release prête !
```

**Durée totale** : 30 secondes + 5 minutes (GitHub Actions)

### Manuellement (DÉTAILLÉ)

```powershell
# 1. Modifier Panosse.csproj (voir ci-dessus)

# 2. Vérifier la compilation
dotnet build

# 3. Commiter
git add Panosse.csproj
git commit -m "Bump version to 1.1.0"
git push

# 4. Créer la release
.\release-simple.ps1 -Version "1.1.0"

# Attendez 5 minutes → Release prête !
```

**Durée totale** : 2-3 minutes + 5 minutes (GitHub Actions)

---

## 🎨 Versions suggérées pour vos prochaines releases

### v1.0.1 - Corrections
**Quoi ajouter** :
- Corrections de bugs
- Petites améliorations
- Optimisations

**Changements dans `.csproj`** :
```xml
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

### v1.1.0 - Nouvelles fonctionnalités
**Quoi ajouter** :
- Nettoyage du cache DNS
- Planification automatique
- Mode silencieux
- Rapport détaillé

**Changements dans `.csproj`** :
```xml
<Version>1.1.0</Version>
<AssemblyVersion>1.1.0.0</AssemblyVersion>
<FileVersion>1.1.0.0</FileVersion>
```

### v1.2.0 - Améliorations UI
**Quoi ajouter** :
- Thème sombre
- Langue anglaise
- Personnalisation

**Changements dans `.csproj`** :
```xml
<Version>1.2.0</Version>
<AssemblyVersion>1.2.0.0</AssemblyVersion>
<FileVersion>1.2.0.0</FileVersion>
```

### v2.0.0 - Refonte majeure
**Quoi ajouter** :
- Nouvelle architecture
- Fonctionnalités avancées
- Changements incompatibles

**Changements dans `.csproj`** :
```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
```

---

## 🔍 Vérifier que tout fonctionne

### Après avoir changé la version

1. **Compilez** :
   ```powershell
   dotnet build
   ```

2. **Lancez** Panosse :
   ```powershell
   .\bin\Debug\net8.0-windows\Panosse.exe
   ```

3. **Ouvrez "À propos"** :
   - Vérifiez que la version affichée est `v1.1.0`

4. **Propriétés du fichier** :
   - Clic droit sur `Panosse.exe` → Propriétés → Détails
   - Version du fichier : `1.1.0.0`
   - Version du produit : `1.1.0`

**Si tout correspond** → Parfait ! ✅

---

## 📋 Checklist de mise à jour de version

Avant de publier une nouvelle version :

- [ ] Toutes les fonctionnalités fonctionnent
- [ ] Les tests sont passés
- [ ] La documentation est à jour (README.md)
- [ ] Le `.csproj` est modifié (3 lignes)
- [ ] Compilation réussie (`dotnet build`)
- [ ] Version affichée correcte ("À propos")
- [ ] Commit effectué
- [ ] Push vers GitHub
- [ ] Release créée avec le script
- [ ] Attendez 5 minutes (GitHub Actions)
- [ ] Vérifiez la release sur GitHub
- [ ] Téléchargez et testez le `.exe`

---

## 💡 Astuces

### Astuce 1 : Utilisez Semantic Versioning

```
MAJOR.MINOR.PATCH

MAJOR : Changements incompatibles
MINOR : Nouvelles fonctionnalités compatibles
PATCH : Corrections de bugs
```

**Exemples** :
- `1.0.0` → `1.0.1` : Correction de bug
- `1.0.1` → `1.1.0` : Nouvelle fonctionnalité
- `1.1.0` → `2.0.0` : Refonte majeure

### Astuce 2 : Versions beta/alpha

Pour des versions de test :
```xml
<Version>1.1.0-beta</Version>
```

Le système de mise à jour ignorera le suffixe lors de la comparaison.

### Astuce 3 : Gardez un changelog

Créez un fichier `CHANGELOG.md` :
```markdown
# Changelog

## [1.1.0] - 2025-01-15
### Ajouté
- Nettoyage du cache DNS
- Mode silencieux

### Corrigé
- Bug de progression à 66%

## [1.0.0] - 2025-01-10
- Version initiale
```

---

## 🎯 Résumé ultra-rapide

### Pour passer à v1.1 (30 secondes)

```powershell
# Option 1 : Script automatique
.\bump-version.ps1 -NewVersion "1.1.0"

# Option 2 : Manuel
# 1. Ouvrir Panosse.csproj
# 2. Changer 1.0.0 → 1.1.0 (3 lignes)
# 3. Sauvegarder
# 4. git add Panosse.csproj
# 5. git commit -m "Bump version to 1.1.0"
# 6. git push
# 7. .\release-simple.ps1 -Version "1.1.0"
```

**C'est tout !** 🎉

---

## ❓ FAQ

### Q : Je dois changer la version à combien d'endroits ?

**R** : **UN SEUL** ! Le fichier `Panosse.csproj`.  
Tout le reste (interface, vérification MAJ) se met à jour automatiquement !

### Q : Que se passe-t-il si j'oublie une des 3 lignes ?

**R** : Pas de gros problème, mais les métadonnées seront incohérentes.  
**Solution** : Utilisez le script `bump-version.ps1` qui change les 3 automatiquement !

### Q : Puis-je sauter des versions ?

**R** : Oui ! Vous pouvez passer de `1.0.0` à `1.5.0` ou `2.0.0`.  
Mais c'est plus clair de suivre Semantic Versioning.

### Q : Combien de temps pour créer une nouvelle version ?

**R** : 
- Modification du `.csproj` : 10 secondes
- Script de release : 10 secondes
- GitHub Actions : 5 minutes
- **Total : ~5 minutes !**

---

## 🎊 Vous êtes prêt !

Votre système est **parfaitement configuré** :

✅ `.csproj` avec balises de version  
✅ Version lue automatiquement dans le code  
✅ Script `bump-version.ps1` pour automatiser  
✅ Script `release-simple.ps1` pour publier  
✅ GitHub Actions pour compiler  
✅ Système de mise à jour automatique  

**Passer à v1.1 sera ultra-simple !** 🚀

---

**Besoin d'aide ? Suivez ce guide étape par étape ! 📝**

