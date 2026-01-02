# 🚀 Publication manuelle de Panosse v2.0.0 sur GitHub

## ✅ État actuel

### Fichiers prêts
- ✅ **Panosse.exe** (76.53 Mo) - Exécutable portable
  - Chemin : `bin\Release\net8.0-windows\win-x64\publish\Panosse.exe`
  - SHA256 : `B7FDEAF45058486A0CD62125EBEDA3F3C170BE45E8EE92B1C549288B2A2BB6D9`

- ✅ **Panosse-Setup-v2.0.0.exe** (73.33 Mo) - Installateur Inno Setup
  - Chemin : `installer\Panosse-Setup-v2.0.0.exe`
  - SHA256 : `6B91FA92B259AE3B9C709213D3867CBEB54D078701F099165145A7F5D30D73F4`

### Git
- ✅ Commits poussés sur GitHub
- ✅ Tag **v2.0.0** créé et poussé

---

## 📝 Création manuelle de la release

### 🔗 Étape 1 : Ouvrir la page de création de release

Ouvrez ce lien dans votre navigateur :
```
https://github.com/barbarom84-ai/panosse/releases/new?tag=v2.0.0
```

---

### 📋 Étape 2 : Remplir les informations

#### **Tag version**
Le tag `v2.0.0` devrait déjà être sélectionné.

#### **Release title**
```
Panosse v2.0.0 - Mémoire Sélective
```

#### **Description** (à copier-coller)

```markdown
# 🧹 Panosse v2.0.0 - Mémoire Sélective

## 🆕 Nouveautés majeures

### 🎯 Raccourci global **Ctrl+Alt+P**
- Nettoyage instantané en arrière-plan depuis n'importe quelle application
- Notification Toast avec espace libéré
- Fonctionne même quand la fenêtre est fermée

### 🔔 Icône System Tray intelligente
- **Icône verte (propre)** : Tout va bien, PC propre
- **Icône rouge (sale)** : Téléchargements encombrés (> 5 Go)
- Menu contextuel : Ouvrir, Nettoyer, Info, Quitter
- Double-clic : Ouvrir Panosse

### 🧠 Surveillance automatique
- Vérification horaire du dossier Téléchargements
- Alerte visuelle si > 5 Go ou fichiers anciens (> 30 jours)
- Clic droit → "Pourquoi l'icône est rouge ?" pour détails

### ⚙️ Lancement au démarrage (optionnel)
- Option cochée par défaut dans l'installateur
- Garantit que **Ctrl+Alt+P** est toujours actif
- Clé de registre : `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

---

## 📦 Fichiers disponibles

### 1️⃣ **Panosse.exe** (Portable)
- Exécutable unique, aucune installation requise
- Taille : ~76 Mo (runtime .NET 8.0 inclus)
- Double-clic pour lancer immédiatement

### 2️⃣ **Panosse-Setup-v2.0.0.exe** (Installateur)
- Installation complète avec raccourcis
- Option "Lancer au démarrage de Windows"
- Désinstallation propre via Paramètres Windows
- Taille : ~73 Mo

---

## ✨ Fonctionnalités complètes

### Nettoyage automatique
- ✅ Corbeille Windows
- ✅ Cache navigateurs (Edge, Chrome, Firefox)
- ✅ Fichiers temporaires système (%TEMP%)
- ✅ Logs Windows (C:\Windows\Logs)
- ✅ Cache miniatures (Thumbnails)
- ✅ Téléchargements anciens (.exe, .msi > 14 jours)
- ✅ Registre Windows (RunMRU, RecentDocs)

### Interface moderne
- Barre de menu professionnelle (Fichier, Outils, Aide)
- Progress bar détaillée avec liste des tâches
- Animations fluides (fade-in, bounce)
- Vérification automatique des mises à jour
- Panneau "À propos" avec version

### Intégration Windows
- System Tray permanent
- Raccourci global **Ctrl+Alt+P**
- Notifications Toast
- Menu contextuel complet
- Lancement au démarrage (optionnel)

---

## 🚀 Installation

### Méthode 1 : Installateur (Recommandé)
1. Téléchargez **Panosse-Setup-v2.0.0.exe**
2. Exécutez l'installateur (droits admin requis)
3. Cochez "Lancer au démarrage" pour profiter de **Ctrl+Alt+P**
4. Profitez !

### Méthode 2 : Portable
1. Téléchargez **Panosse.exe**
2. Double-cliquez pour lancer
3. Aucune installation, aucun résidu

---

## 💡 Utilisation

### Nettoyage manuel
1. Ouvrez Panosse
2. Cliquez sur "Passer la panosse"
3. Observez le nettoyage en temps réel

### Nettoyage instantané
- Appuyez sur **Ctrl+Alt+P** n'importe quand
- Panosse nettoie en arrière-plan
- Notification Toast à la fin

### Surveillance
- Icône System Tray change de couleur si besoin
- Clic droit → "Pourquoi l'icône est rouge ?"
- Détails sur l'encombrement du dossier Téléchargements

---

## 📋 Configuration requise

- **OS** : Windows 10 / 11 (64-bit)
- **RAM** : 2 Go minimum
- **Espace disque** : 100 Mo
- **Droits** : Administrateur (pour nettoyage système)

---

## 🔒 Sécurité

- ✅ Nettoyage uniquement de fichiers temporaires et obsolètes
- ✅ Aucun fichier système critique touché
- ✅ Gestion robuste des erreurs (accès refusés silencieux)
- ✅ Open source : code vérifiable sur GitHub

---

## 📝 Notes de version

### v2.0.0 (2025-01-02)
- 🆕 Raccourci global Ctrl+Alt+P
- 🆕 Icône System Tray intelligente (propre/sale)
- 🆕 Surveillance automatique Téléchargements
- 🆕 Option lancement au démarrage
- 🆕 Menu contextuel System Tray
- 🆕 Notification Toast
- 🆕 Barre de menu professionnelle
- 🔧 Amélioration interface utilisateur
- 🔧 Optimisation performance
- 🐛 Corrections bugs mineurs

---

## 🆘 Support

- **GitHub** : [barbarom84-ai/panosse](https://github.com/barbarom84-ai/panosse)
- **Issues** : [Signaler un bug](https://github.com/barbarom84-ai/panosse/issues)

---

## 📄 Licence

Open Source - Utilisation libre

---

## 🎉 Merci d'utiliser Panosse !

**La serpillère numérique qui garde votre PC tout propre ! 🧹✨**
```

---

### 📤 Étape 3 : Upload des fichiers

Faites glisser ou cliquez sur "Attach binaries..." pour ajouter :

1. **`bin\Release\net8.0-windows\win-x64\publish\Panosse.exe`**
   - Renommer en : `Panosse.exe`

2. **`installer\Panosse-Setup-v2.0.0.exe`**
   - Garder le nom : `Panosse-Setup-v2.0.0.exe`

---

### ✅ Étape 4 : Options

- ✅ Cochez **"Set as the latest release"**
- ❌ Ne cochez PAS "Set as a pre-release"

---

### 🚀 Étape 5 : Publier

Cliquez sur **"Publish release"**

---

## 🎉 Résultat

Votre release sera visible à :
```
https://github.com/barbarom84-ai/panosse/releases/tag/v2.0.0
```

---

## 📊 Récapitulatif de la publication

### ✅ Ce qui a été fait automatiquement

1. ✅ Compilation du projet en Release (single-file)
   - `Panosse.exe` : 76.53 Mo
   - SHA256 : `B7FDEAF45058486A0CD62125EBEDA3F3C170BE45E8EE92B1C549288B2A2BB6D9`

2. ✅ Création de l'installateur Inno Setup
   - `Panosse-Setup-v2.0.0.exe` : 73.33 Mo
   - SHA256 : `6B91FA92B259AE3B9C709213D3867CBEB54D078701F099165145A7F5D30D73F4`

3. ✅ Commit et push des modifications

4. ✅ Création et push du tag Git `v2.0.0`

### 🔧 À faire manuellement

5. ⏳ **Création de la release GitHub** (en cours)
   - Ouvrir : https://github.com/barbarom84-ai/panosse/releases/new?tag=v2.0.0
   - Remplir titre et description (voir ci-dessus)
   - Upload 2 fichiers
   - Publier

---

## 💡 Conseils

### Téléchargement des fichiers
Les fichiers à uploader sont prêts dans :
- `bin\Release\net8.0-windows\win-x64\publish\Panosse.exe`
- `installer\Panosse-Setup-v2.0.0.exe`

### Vérification
Après publication, testez les liens de téléchargement :
- Cliquez sur chaque asset
- Vérifiez que le téléchargement démarre correctement

### Annonce
Une fois publié, vous pouvez :
- Partager le lien : `https://github.com/barbarom84-ai/panosse/releases/tag/v2.0.0`
- Mettre à jour le README avec la dernière version
- Annoncer sur vos réseaux sociaux

---

## 🔄 Automatisation future (optionnel)

Pour automatiser les prochaines releases, installez `gh CLI` :

1. Téléchargez : https://cli.github.com/
2. Installez : `winget install --id GitHub.cli`
3. Authentifiez : `gh auth login`
4. Utilisez : `.\creer-release-v2.0.0.ps1`

---

## 🎊 Félicitations !

Vous êtes sur le point de publier Panosse v2.0.0 avec toutes ses nouveautés !

**Bonne publication ! 🧹✨**

