# 🔄 Remplacement de la Release v2.0.0 sur GitHub

## ✅ Ce qui a été fait automatiquement

- ✅ Tag local v2.0.0 supprimé
- ✅ Tag distant v2.0.0 supprimé
- ✅ Nouveau tag v2.0.0 créé (avec commit corrigé)
- ✅ Nouveau tag v2.0.0 envoyé sur GitHub

---

## 🛠️ Ce qu'il reste à faire MANUELLEMENT

**`gh CLI` n'est pas installé**, donc vous devez supprimer et recréer la release **manuellement sur GitHub**.

---

## 📋 GUIDE ÉTAPE PAR ÉTAPE

### Étape 1 : Aller sur GitHub

Ouvrez votre navigateur :

```
https://github.com/barbarom84-ai/panosse/releases
```

---

### Étape 2 : Supprimer l'ancienne release v2.0.0

1. **Trouvez la release "v2.0.0"** dans la liste
2. **Cliquez sur le titre** pour ouvrir la page de la release
3. **Cliquez sur le bouton "Delete"** (🗑️) en haut à droite
4. **Confirmez la suppression**

⚠️ **IMPORTANT** : Seule la **release** sera supprimée, pas le tag (déjà fait automatiquement).

---

### Étape 3 : Créer la nouvelle release v2.0.0

1. **Cliquez sur "Draft a new release"** (bouton vert en haut à droite)

2. **Choisir le tag** :
   - Cliquez sur "Choose a tag"
   - Sélectionnez **"v2.0.0"** (celui qui vient d'être créé)

3. **Titre de la release** :
   ```
   Panosse v2.0.0 - Mémoire Sélective 🧹✨
   ```

4. **Description de la release** :

```markdown
# 🎉 Panosse v2.0.0 - Mémoire Sélective

Version majeure apportant des fonctionnalités avancées de surveillance et d'automatisation !

---

## ✨ Nouvelles fonctionnalités

### 🔥 Raccourci global Ctrl+Alt+P
- Nettoyage en arrière-plan sans ouvrir la fenêtre
- Toast notification avec résultat
- Son de succès

### 🚦 Icône System Tray intelligente
- **Icône propre** (`panosse_propre.ico`) : Tout va bien
- **Icône sale** (`panosse_sale.ico`) : Téléchargements encombré
- Changement automatique selon l'état
- Reset automatique après nettoyage

### 📊 Surveillance automatique "Mémoire Sélective"
- Vérification toutes les heures du dossier Téléchargements
- Alerte si **> 5 Go** OU **fichiers > 200 Mo non modifiés depuis > 30 jours**
- Menu contextuel "Pourquoi l'icône est rouge ?" avec détails
- Très léger en ressources (asynchrone)

### 🚀 Lancement au démarrage Windows
- Option dans l'installateur
- Raccourci Ctrl+Alt+P toujours actif
- Application en arrière-plan dans le System Tray

### 🎨 Barre de menu professionnelle
- **Fichier** : Actualiser détection, Quitter
- **Outils** : Vérifier mises à jour, Ouvrir dépôt GitHub
- **Aide** : À propos de Panosse

### 📦 Menu contextuel System Tray complet
- Ouvrir Panosse
- Passer la panosse maintenant
- Pourquoi l'icône est rouge ? (si encombré)
- Quitter

---

## 🛡️ Améliorations techniques

### Système de logging complet
- **`panosse_debug.log`** : Trace détaillée de chaque étape au démarrage
- **`panosse_crash.log`** : Détails complets si erreur
- Créés sur le Bureau si nécessaire
- Facilite le diagnostic et le support

### Gestion d'erreurs robuste
- Try-catch dans toutes les méthodes critiques
- Gestionnaires d'exceptions globaux (`App.xaml.cs`)
- Messages d'erreur conviviaux pour l'utilisateur

### Ressources embarquées
- Images via `pack://application:,,,/`
- Compatible single-file
- Icônes multiples incluses

### Optimisations
- Single-file avec compression
- ReadyToRun pour démarrage plus rapide
- Taille optimisée (76.78 Mo)

---

## 🐛 Corrections v2.0.0

### Bug #1 : Crash silencieux au démarrage
- **Cause** : `InitialiserSystemTray()` appelé dans le constructeur avant chargement complet
- **Solution** : Déplacé vers `MainWindow_Loaded`
- **Impact** : Application démarre correctement à 100%

### Bug #2 : Erreur chargement images
- **Cause** : Chemins relatifs (`assets/panosse.png`) incompatibles avec single-file (baseUri null)
- **Erreur** : `Value cannot be null (Parameter 'path1')`
- **Solution** : Utilisation de `pack://application:,,,/assets/` pour toutes les images
- **Impact** : Toutes les images s'affichent correctement

---

## 📥 Fichiers disponibles

### Panosse.exe (Portable)
- **Taille** : 76.78 Mo
- **SHA256** : `007F4504FB640A628CBCAC0572166AE0D0B87D116FDE4DC2C93F0FFC62AA8FDC`
- Single-file (pas d'installation requise)
- Self-contained (.NET 8.0 inclus)
- Compatible Windows 10/11 64-bit

### Panosse-Setup-v2.0.0.exe (Installateur)
- **Taille** : 73.33 Mo
- **SHA256** : `4D5A81749441C78A3B86463375164D8EC3D2C47FED109BE2830CA87AE1216C9C`
- Installation complète avec raccourcis
- Option "Lancer au démarrage de Windows"
- Désinstallation propre

---

## 🧪 Tests effectués

- ✅ Version Debug : Fonctionne
- ✅ Version Release : Fonctionne
- ✅ Version single-file : Fonctionne
- ✅ Installateur : Créé avec succès
- ✅ Démarrage : OK (sans crash)
- ✅ System Tray : Icône visible
- ✅ Menu contextuel : Opérationnel
- ✅ Fermeture fenêtre : Cache l'app (ne ferme pas)
- ✅ Nettoyage : Fonctionnel
- ✅ Logging : Logs créés correctement
- ✅ Ctrl+Alt+P : Enregistré et fonctionnel
- ✅ Changement icône : Opérationnel

---

## 📖 Documentation

- [README.md](https://github.com/barbarom84-ai/panosse#readme) : Guide complet
- Logs intégrés pour diagnostic rapide
- Code commenté et structuré

---

## 🎯 Utilisation

### Première installation
1. Téléchargez **Panosse-Setup-v2.0.0.exe**
2. Exécutez l'installateur
3. **Cochez "Lancer au démarrage de Windows"** (recommandé)
4. Terminez l'installation

### Utilisation quotidienne
- **Ctrl+Alt+P** : Nettoyage rapide en arrière-plan
- **Double-clic sur l'icône Tray** : Ouvrir la fenêtre
- **Clic droit sur l'icône Tray** : Menu contextuel
- **Icône rouge ?** : Clic droit → "Pourquoi l'icône est rouge ?"

---

## 🔄 Migration depuis v1.x

Si vous avez une version antérieure :
1. **Désinstallez l'ancienne version** (Paramètres Windows → Applications)
2. **Installez v2.0.0**
3. **Cochez "Lancer au démarrage"** pour profiter du raccourci Ctrl+Alt+P

⚠️ Aucune migration de données nécessaire (Panosse ne stocke pas de données).

---

## 🐞 Signaler un bug

Si vous rencontrez un problème :
1. Vérifiez les fichiers `panosse_debug.log` et `panosse_crash.log` sur votre Bureau
2. Ouvrez un [Issue sur GitHub](https://github.com/barbarom84-ai/panosse/issues)
3. Joignez les logs si disponibles

---

## 💝 Remerciements

Merci d'utiliser Panosse ! Cette version représente des semaines de développement et de tests.

**Panosse v2.0.0 - La serpillère qui pense à vous ! 🧹✨**
```

5. **Ajouter les fichiers** :
   - Cliquez sur "Attach binaries by dropping them here or selecting them"
   - **Glissez-déposez** ou sélectionnez ces fichiers :
     ```
     C:\Users\marco\Cursor Workplace\panosse\bin\Release\net8.0-windows\win-x64\publish\Panosse.exe
     C:\Users\marco\Cursor Workplace\panosse\installer\Panosse-Setup-v2.0.0.exe
     ```

6. **Options** :
   - ☑️ **Cochez** "Set as the latest release"
   - ☐ Ne PAS cocher "Set as a pre-release"

7. **Publier** :
   - Cliquez sur **"Publish release"** (bouton vert)

---

## ✅ Vérification finale

Une fois la release publiée :

1. **Vérifiez que les fichiers sont bien présents** :
   - Panosse.exe (76.78 Mo)
   - Panosse-Setup-v2.0.0.exe (73.33 Mo)

2. **Testez le téléchargement** :
   - Cliquez sur un fichier pour le télécharger
   - Vérifiez que le téléchargement fonctionne

3. **Vérifiez la vérification de mise à jour** :
   - Lancez Panosse
   - Allez dans le menu **Aide → Vérifier les mises à jour**
   - Devrait afficher "✅ Version à jour"

---

## 🎉 C'est terminé !

La release v2.0.0 est maintenant **complètement remplacée** sur GitHub avec :
- ✅ Tag corrigé
- ✅ Fichiers fonctionnels
- ✅ Description complète
- ✅ Documentation à jour

**Félicitations ! Panosse v2.0.0 est officiellement publié ! 🚀🧹✨**

---

## 🔗 Liens utiles

- **Page des releases** : https://github.com/barbarom84-ai/panosse/releases
- **Release v2.0.0** : https://github.com/barbarom84-ai/panosse/releases/tag/v2.0.0
- **Dépôt GitHub** : https://github.com/barbarom84-ai/panosse

---

## 📞 Besoin d'aide ?

Si vous rencontrez un problème lors de la création manuelle de la release, dites-le moi et je vous guiderai ! 😊

