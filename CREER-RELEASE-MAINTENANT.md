# 🚨 SOLUTION IMMÉDIATE : Créer la release manuellement

## 🔍 Le problème

Le workflow GitHub Actions ne fonctionne pas correctement (actions dépréciées).
Pour débloquer immédiatement votre système de mise à jour, créez la release **manuellement** en 2 minutes !

---

## ✅ SOLUTION EN 5 ÉTAPES

### Étape 1 : Ouvrir la page de création de release

👉 **Cliquez sur ce lien** : 
**https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.0.0**

---

### Étape 2 : Remplir le formulaire

Sur la page GitHub :

1. **Tag** : `v1.0.0` (déjà rempli)
2. **Title** : `Panosse v1.0.0`
3. **Description** : Copiez-collez le texte ci-dessous ⬇️

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

---

### Étape 3 : Uploader les fichiers

En bas de la page GitHub, dans la section "Attach binaries" :

1. Glissez-déposez le fichier : **`release-manual\Panosse-v1.0.0.exe`**
2. Glissez-déposez le fichier : **`release-manual\SHA256SUMS.txt`**

Ou cliquez sur "choose them" et sélectionnez les 2 fichiers.

---

### Étape 4 : Publier

Cliquez sur le bouton vert **"Publish release"** en bas de la page.

---

### Étape 5 : Tester Panosse

1. **Lancez** `Panosse.exe`
2. **Cliquez** sur le bouton `ℹ️` (À propos)
3. **Cliquez** sur `🔍 Vérifier les mises à jour`
4. **Résultat attendu** : `✅ Version à jour` 🎉

---

## 📁 Emplacement des fichiers

Les fichiers à uploader sont dans le dossier :
```
C:\Users\marco\Cursor Workplace\panosse\release-manual\
```

Fichiers :
- ✅ `Panosse-v1.0.0.exe` (≈ 70 MB)
- ✅ `SHA256SUMS.txt`

---

## ⏱️ Temps estimé

**2 minutes maximum** ! C'est très rapide.

---

## 🎯 Après la publication

Votre release sera visible sur :
**https://github.com/barbarom84-ai/panosse/releases**

Et l'API GitHub répondra correctement :
```
https://api.github.com/repos/barbarom84-ai/panosse/releases/latest
```

**Votre système de mise à jour automatique fonctionnera ! ✨**

---

## ❓ Besoin d'aide ?

Si vous avez un problème, dites-le moi et je vous aiderai !

---

## 🔄 Pour les prochaines releases

Une fois la première release créée, le workflow GitHub Actions devrait fonctionner correctement pour les suivantes.

Vous pourrez simplement utiliser :
```powershell
.\bump-version.ps1 -NewVersion "1.0.1"
```

Et tout se fera automatiquement ! 🚀

