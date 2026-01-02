# 🧪 TEST DU SYSTÈME DE MISE À JOUR

## ✅ Version 1.1.1 prête !

J'ai préparé la version 1.1.1 pour tester le système de mise à jour automatique.

**Dossier** : `release-v1.1.1\`
- ✅ Panosse-v1.1.1.exe
- ✅ SHA256SUMS.txt

---

## ⚠️ IMPORTANT : Prérequis

Pour que le test fonctionne, il faut **OBLIGATOIREMENT** :

1. ✅ Avoir créé la release **v1.1.0** sur GitHub **AVANT**
2. ✅ Attendre quelques secondes que l'API GitHub se mette à jour

**Pourquoi ?** Panosse v1.1.0 va vérifier sur GitHub quelle est la dernière version disponible. Si v1.1.0 n'existe pas sur GitHub, l'API retournera 404 et vous aurez "Vérification impossible".

---

## 🎯 Plan de test complet

### Phase 1 : Créer les releases sur GitHub (si pas encore fait)

#### 1.1 - Créer v1.1.0 MAINTENANT
👉 **https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.1.0**

- **Title** : `Panosse v1.1.0`
- **Fichiers** : Uploadez depuis `release-v1.1.0\`
- **Description** : Voir `CREER-LES-2-RELEASES.md`

⏱️ **Attendez 30 secondes** après la publication pour que l'API se mette à jour.

#### 1.2 - Créer v1.1.1 ensuite
👉 **https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.1.1**

- **Title** : `Panosse v1.1.1`
- **Fichiers** : Uploadez depuis `release-v1.1.1\`
- **Description** : Voir ci-dessous ⬇️

```markdown
## 🧹 Panosse v1.1.1

**Version de test pour le système de mise à jour automatique**

### 📦 Installation

Téléchargez `Panosse-v1.1.1.exe` ci-dessous et lancez-le.

**Aucune installation requise** - Version portable complète.

### ✨ Contenu de v1.1.1

Cette version est identique à v1.1.0, créée uniquement pour tester le système de mise à jour automatique.

### 🔄 Test de mise à jour

Si vous lancez Panosse v1.1.0 après la publication de cette release, l'application devrait :
1. Détecter automatiquement v1.1.1 au démarrage
2. Afficher une barre verte en haut "Une nouvelle version est disponible !"
3. Permettre de télécharger et installer v1.1.1 en un clic

### ⚠️ Prérequis

- **Windows 10/11** (64-bit)
- **Droits administrateur** (certaines fonctions)
- **.NET 8.0** inclus (self-contained)

### 🔐 Checksum SHA256

```
28B71B18D241DD106A7212C9ED078A33CAA0872B0A0E0B4A0D4380B930EC362F
```
```

---

### Phase 2 : Vérifier que l'API fonctionne

Après avoir créé les releases, testez l'API :

```powershell
# Test de l'API
$response = Invoke-RestMethod -Uri "https://api.github.com/repos/barbarom84-ai/panosse/releases/latest" -Headers @{"User-Agent"="Test"}
Write-Host "Derniere version disponible : $($response.tag_name)"
Write-Host "URL de telechargement : $($response.assets[0].browser_download_url)"
```

**Résultat attendu** : `v1.1.1`

---

### Phase 3 : Test du système de mise à jour

#### Scénario 1 : Détection automatique au démarrage

1. **Lancez** `Panosse-v1.1.0.exe` (depuis `release-v1.1.0\`)
2. **Attendez** 2-3 secondes
3. **Résultat attendu** :
   - ✅ Barre verte en haut : "🔔 Une nouvelle version est disponible !"
   - ✅ Bouton "Mettre à jour"

#### Scénario 2 : Vérification manuelle

1. **Lancez** `Panosse-v1.1.0.exe`
2. **Cliquez** sur `ℹ️` (À propos)
3. **Cliquez** sur `🔍 Vérifier les mises à jour`
4. **Résultat attendu** :
   - ✅ MessageBox : "Une nouvelle version de Panosse est disponible ! Version actuelle : 1.1.0 / Nouvelle version : 1.1.1"
   - ✅ Bouton "Oui" pour télécharger

#### Scénario 3 : Téléchargement et installation

1. **Dans la barre verte**, cliquez sur **"Mettre à jour"**
2. **Résultat attendu** :
   - ✅ Message change en "Téléchargement de la mise à jour..."
   - ✅ Barre de progression apparaît et progresse (0% → 100%)
   - ✅ MessageBox "Mise à jour prête"
3. **Cliquez** sur "OK"
4. **Résultat attendu** :
   - ✅ Panosse se ferme
   - ✅ Une fenêtre CMD apparaît brièvement
   - ✅ Panosse v1.1.1 se relance automatiquement
5. **Vérifiez** dans "À propos" : Version doit être **v1.1.1**

#### Scénario 4 : Vérification après mise à jour

1. **Dans Panosse v1.1.1**, cliquez sur `ℹ️` (À propos)
2. **Cliquez** sur `🔍 Vérifier les mises à jour`
3. **Résultat attendu** :
   - ✅ Bouton devient "✅ Version à jour"
   - ✅ MessageBox "Vous utilisez déjà la dernière version !"

---

## 📊 Checklist de test

### Avant de tester
- [ ] Release v1.1.0 créée sur GitHub
- [ ] Release v1.1.1 créée sur GitHub
- [ ] API GitHub retourne bien v1.1.1
- [ ] Attente de 30 secondes après création des releases

### Tests à effectuer
- [ ] ✅ Détection automatique au démarrage (barre verte)
- [ ] ✅ Vérification manuelle depuis "À propos"
- [ ] ✅ MessageBox avec proposition de MAJ
- [ ] ✅ Téléchargement avec barre de progression
- [ ] ✅ Fermeture automatique de Panosse
- [ ] ✅ Remplacement de l'exécutable
- [ ] ✅ Relance automatique de Panosse
- [ ] ✅ Version mise à jour (v1.1.1)
- [ ] ✅ Vérification "À jour" après mise à jour

---

## 🐛 Problèmes possibles et solutions

### Problème 1 : "Vérification impossible"
**Cause** : Releases pas encore créées sur GitHub ou API pas à jour  
**Solution** : Créez les releases et attendez 30 secondes

### Problème 2 : "Version à jour" alors que v1.1.1 existe
**Cause** : Vous lancez déjà Panosse v1.1.1  
**Solution** : Lancez `release-v1.1.0\Panosse-v1.1.0.exe`

### Problème 3 : Téléchargement échoue
**Cause** : URL de téléchargement incorrecte ou fichier non uploadé  
**Solution** : Vérifiez que les assets sont bien présents dans la release GitHub

### Problème 4 : Panosse ne redémarre pas
**Cause** : Script batch échoue ou droits insuffisants  
**Solution** : 
1. Vérifiez dans `%TEMP%` si le fichier `Panosse-v1.1.1.exe` est téléchargé
2. Relancez Panosse manuellement depuis `release-v1.1.1\`

---

## 🎯 Résultat attendu final

Si tout fonctionne correctement, voici ce qui devrait se passer :

```
1. Vous lancez Panosse v1.1.0
2. Barre verte apparaît : "Nouvelle version disponible !"
3. Vous cliquez "Mettre à jour"
4. Téléchargement : 0% → 100% (≈ 10 secondes)
5. MessageBox : "Mise à jour prête"
6. Panosse se ferme
7. CMD flash rapidement (remplacement)
8. Panosse v1.1.1 se relance
9. Dans "À propos" : v1.1.1 ✅
```

**C'est magique ! ✨**

---

## 📝 Commandes rapides

### Créer les releases rapidement

```powershell
# Ouvrir les pages de création
Start-Process "https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.1.0"
Start-Process "https://github.com/barbarom84-ai/panosse/releases/new?tag=v1.1.1"

# Ouvrir les dossiers des fichiers
Start-Process "release-v1.1.0"
Start-Process "release-v1.1.1"
```

### Vérifier l'API

```powershell
$r = Invoke-RestMethod -Uri "https://api.github.com/repos/barbarom84-ai/panosse/releases/latest" -Headers @{"User-Agent"="Test"}
Write-Host "Derniere version : $($r.tag_name)"
```

### Lancer Panosse v1.1.0 pour tester

```powershell
cd release-v1.1.0
.\Panosse-v1.1.0.exe
```

---

## ✨ Après le test

Une fois que tout fonctionne :

1. ✅ Vous pouvez supprimer la release v1.1.1 (c'était juste pour tester)
2. ✅ Ou la garder comme version "stable"
3. ✅ Créer v1.2.0 avec de vraies nouveautés quand vous voulez

**Votre système de mise à jour automatique est 100% opérationnel ! 🚀**

---

**Bon test ! 🧪**

