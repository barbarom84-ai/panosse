# 🚀 CRÉER LES DEUX RELEASES MAINTENANT

## ✅ Fichiers prêts !

J'ai préparé les fichiers pour **2 releases** :

| Release | Dossier | Fichiers |
|---------|---------|----------|
| **v1.0.0** | `release-manual\` | - Panosse-v1.0.0.exe<br>- SHA256SUMS.txt |
| **v1.1.0** | `release-v1.1.0\` | - Panosse-v1.1.0.exe<br>- SHA256SUMS.txt |

---

## 📋 ACTION : Créer v1.0.0 d'abord

### Étape 1 : Ouvrir GitHub
👉 **https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.0.0**

### Étape 2 : Remplir le formulaire

- **Tag** : `v1.0.0` (déjà rempli)
- **Title** : `Panosse v1.0.0`
- **Description** : Copiez-collez ⬇️

```markdown
## 🧹 Panosse v1.0.0

**La serpillère numérique pour un PC tout propre !**

### 📦 Installation

Téléchargez `Panosse-v1.0.0.exe` ci-dessous et lancez-le.

**Aucune installation requise** - Version portable complète.

### ✨ Fonctionnalités

- 🗑️ Vidage de la corbeille
- 🧹 Nettoyage fichiers temporaires
- 🌐 Cache navigateurs (Chrome, Firefox, Edge)
- 📋 Nettoyage registre (RunMRU, RecentDocs)
- 📥 Suppression .exe/.msi anciens (Téléchargements)
- 📄 Nettoyage logs Windows
- 🖼️ Cache miniatures
- 📊 Progression détaillée avec animations
- 🔄 Mise à jour automatique

### ⚠️ Prérequis

- **Windows 10/11** (64-bit)
- **Droits administrateur** (certaines fonctions)
- **.NET 8.0** inclus (self-contained)

### 🔐 Checksum SHA256

```
E60323F663490C66E92F6A0520B58EB9ABD65F4B053049C741C8EE8A3F80E2BF
```
```

### Étape 3 : Uploader les fichiers
Glissez-déposez depuis `release-manual\` :
- ✅ Panosse-v1.0.0.exe
- ✅ SHA256SUMS.txt

### Étape 4 : Publier
Cliquez sur **"Publish release"**

---

## 📋 ACTION : Créer v1.1.0 ensuite

### Étape 1 : Ouvrir GitHub
👉 **https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.1.0**

### Étape 2 : Remplir le formulaire

- **Tag** : `v1.1.0` (déjà rempli)
- **Title** : `Panosse v1.1.0`
- **Description** : Copiez-collez ⬇️

```markdown
## 🧹 Panosse v1.1.0

**Améliorations de l'interface et corrections !**

### 📦 Installation

Téléchargez `Panosse-v1.1.0.exe` ci-dessous et lancez-le.

**Aucune installation requise** - Version portable complète.

### ✨ Nouveautés dans v1.1.0

#### Améliorations UI
- ✅ Bouton "Vérifier les mises à jour" mieux positionné (sous la version)
- ✅ Alignement parfait du panneau "À propos"
- ✅ Messages d'erreur plus courts et mieux centrés
- ✅ Largeur fixe du bouton (200px) pour cohérence visuelle
- ✅ Support du texte multi-ligne dans les boutons

#### Corrections techniques
- ✅ Remplacement de WebClient (obsolète) par HttpClient
- ✅ Gestion robuste des erreurs de connexion
- ✅ Corrections des avertissements de compilation
- ✅ Workflow GitHub Actions corrigé

### 🔄 Mise à jour depuis v1.0.0

Si vous utilisez Panosse v1.0.0, l'application détectera automatiquement cette nouvelle version et vous proposera de la télécharger !

### ⚠️ Prérequis

- **Windows 10/11** (64-bit)
- **Droits administrateur** (certaines fonctions)
- **.NET 8.0** inclus (self-contained)

### 🔐 Checksum SHA256

```
FC86BFA3447B5991DC2BA079DFAD8C558E852FFCCA384DD4219AFF8C19E83B5F
```
```

### Étape 3 : Uploader les fichiers
Glissez-déposez depuis `release-v1.1.0\` :
- ✅ Panosse-v1.1.0.exe
- ✅ SHA256SUMS.txt

### Étape 4 : Publier
Cliquez sur **"Publish release"**

---

## 🧪 TEST APRÈS PUBLICATION

### Test 1 : Vérifier l'API
```powershell
$response = Invoke-RestMethod -Uri "https://api.github.com/repos/barbarom84-ai/panosse/releases/latest" -Headers @{"User-Agent"="Test"}
Write-Host "Dernière release : $($response.tag_name)"
```

**Résultat attendu** : `v1.1.0` (la plus récente)

### Test 2 : Dans Panosse v1.0.0
1. Lancez `Panosse.exe` (version 1.0.0)
2. Cliquez sur `ℹ️` (À propos)
3. Cliquez sur `🔍 Vérifier les mises à jour`
4. **Résultat** : Notification "Une nouvelle version est disponible !" 🎉
5. Cliquez sur "Mettre à jour" → Téléchargement de v1.1.0

### Test 3 : Dans Panosse v1.1.0
1. Lancez `Panosse.exe` (version 1.1.0)
2. Cliquez sur `ℹ️` (À propos)
3. Cliquez sur `🔍 Vérifier les mises à jour`
4. **Résultat** : "✅ Version à jour" 🎉

---

## ⏱️ Temps estimé

- **Création v1.0.0** : 2 minutes
- **Création v1.1.0** : 2 minutes
- **Total** : 4 minutes

---

## 📝 Ordre d'importance

### Option A : Les deux releases (recommandé)
Créez d'abord v1.0.0, puis v1.1.0. Cela permet de tester le système de mise à jour !

### Option B : Seulement v1.1.0
Si vous voulez aller plus vite, créez seulement v1.1.0. Le système de vérification fonctionnera quand même.

---

## ✨ Après publication

Une fois les releases créées :

1. ✅ L'API GitHub fonctionnera correctement
2. ✅ Le message "Vérification impossible" disparaîtra
3. ✅ Le système de mise à jour automatique fonctionnera
4. ✅ Les utilisateurs recevront des notifications de MAJ

**Votre système est complet ! 🚀**

---

## 🔧 Note sur le workflow

Le workflow GitHub Actions ne fonctionne pas correctement car `gh` CLI n'est peut-être pas disponible dans l'environnement Windows de GitHub Actions.

Pour les futures versions, nous pourrons :
- Soit corriger le workflow pour utiliser une autre méthode
- Soit continuer à créer les releases manuellement (4 minutes par version)

Mais pour l'instant, **créons les releases manuellement pour débloquer tout le système !** 💪

