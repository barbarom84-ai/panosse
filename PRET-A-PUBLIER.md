# 🚀 Configuration GitHub pour barbarom84-ai

## ✅ Configuration terminée

Votre dépôt Git est configuré pour :
- **URL** : https://github.com/barbarom84-ai/Panosse.git
- **Branche** : main
- **Commits** : 2 (Initialisation + Documentation Git)

---

## 📋 ÉTAPES SUIVANTES

### 1️⃣ Créer le dépôt sur GitHub

**Allez sur** : https://github.com/new

Remplissez :
- **Repository name** : `Panosse`
- **Description** : `La serpillère numérique pour un PC tout propre`
- **Visibilité** : Public ou Privé (votre choix)
- ❌ **NE COCHEZ RIEN** (pas de README, pas de .gitignore, pas de license)

Cliquez **"Create repository"**

---

### 2️⃣ Créer un Personal Access Token

GitHub n'accepte plus les mots de passe simples. Créez un token :

1. **Allez sur** : https://github.com/settings/tokens
2. Cliquez **"Generate new token"** → **"Generate new token (classic)"**
3. **Note** : `Panosse-Push`
4. **Expiration** : 90 days (ou No expiration)
5. **Cochez** : ☑️ `repo` (Full control of private repositories)
6. Cliquez **"Generate token"**
7. **COPIEZ LE TOKEN** ⚠️ Vous ne le verrez qu'une seule fois !

Exemple de token : `ghp_1234567890abcdefghijklmnopqrstuvwxyz`

---

### 3️⃣ Pousser vers GitHub

Une fois le dépôt créé sur GitHub et le token copié :

```powershell
cd "C:\Users\marco\Cursor Workplace\panosse"
& "C:\Program Files\Git\bin\git.exe" push -u origin main
```

**Windows vous demandera** :
- **Username** : `barbarom84-ai`
- **Password** : Collez votre Personal Access Token (pas votre mot de passe GitHub !)

Windows enregistrera vos identifiants automatiquement.

---

### 4️⃣ Vérifier sur GitHub

Après le push, allez sur :
- https://github.com/barbarom84-ai/Panosse

Vous devriez voir :
- ✅ 22 fichiers
- ✅ 2 commits
- ✅ README.md affiché
- ✅ Icônes et assets
- ✅ Code source complet

---

## 🎁 Créer une Release v1.0.0 (Optionnel)

Une fois le code poussé :

1. Sur GitHub, onglet **"Releases"**
2. **"Create a new release"**
3. **Tag** : `v1.0.0`
4. **Title** : `Panosse v1.0.0 - Version initiale`
5. **Description** :

```markdown
## 🎉 Première version de Panosse !

**La serpillère numérique pour un PC tout propre.**

### ✨ Fonctionnalités

- 🗑️ Vidage automatique de la corbeille
- 🧹 Nettoyage des fichiers temporaires Windows
- 🌐 Nettoyage du cache des navigateurs
- 📋 Nettoyage du registre (RunMRU, RecentDocs)
- 📥 Suppression des .exe/.msi anciens dans Téléchargements
- 📄 Nettoyage des logs Windows
- 🖼️ Nettoyage du cache des miniatures
- 📊 Barre de progression détaillée
- ℹ️ Fenêtre "À propos" personnalisée
- ✨ Animations fluides

### 🛠️ Technologies

- C# / WPF / .NET 8.0
- Interface Material Design
- Animations WPF

### 📦 Installation

Téléchargez l'installateur ou la version portable ci-dessous.

### 🔐 Checksums SHA256

**Installateur** : 88D2B83C3BAF38B82E415232D8FAB0F02F557A722D4093DB4CAB7B790C43BF9B
**Portable** : 75E1E9502CC0B2FAC01D940DEC2A4344B32555C06469731C8E2BFA0786A3FACC
```

6. **Uploader** (si disponibles) :
   - `Panosse-Setup-v1.0.0.exe` (installateur)
   - `Panosse.exe` (version portable)
7. Cliquez **"Publish release"**

---

## 📝 Commandes Git pour plus tard

### Vérifier l'état

```powershell
& "C:\Program Files\Git\bin\git.exe" status
```

### Ajouter des modifications

```powershell
& "C:\Program Files\Git\bin\git.exe" add .
```

### Commiter

```powershell
& "C:\Program Files\Git\bin\git.exe" commit -m "Description de la modification"
```

### Pousser

```powershell
& "C:\Program Files\Git\bin\git.exe" push
```

### Voir l'historique

```powershell
& "C:\Program Files\Git\bin\git.exe" log --oneline --graph
```

---

## 🆘 Problèmes courants

### ❌ "repository not found"
→ Le dépôt n'existe pas encore sur GitHub. Créez-le d'abord sur https://github.com/new

### ❌ "authentication failed"
→ Vous avez utilisé votre mot de passe au lieu du Personal Access Token. Créez un token sur https://github.com/settings/tokens

### ❌ "Permission denied"
→ Le token n'a pas les bonnes permissions. Recréez-le avec `repo` coché.

### ❌ "fatal: refusing to merge unrelated histories"
→ Vous avez coché "Add README" lors de la création. Supprimez le dépôt et recréez-le sans rien cocher.

---

## 🎯 Récapitulatif

| Étape | Statut |
|-------|--------|
| ✅ Git installé | Terminé |
| ✅ Repository initialisé | Terminé |
| ✅ .gitignore créé | Terminé |
| ✅ Premier commit | Terminé |
| ✅ Remote configuré | Terminé |
| ⏳ Créer dépôt GitHub | **À FAIRE** |
| ⏳ Créer token | **À FAIRE** |
| ⏳ Push vers GitHub | **À FAIRE** |

---

**🚀 Prêt à publier ! Créez le dépôt sur GitHub et lancez le push !**

URL de votre futur dépôt : https://github.com/barbarom84-ai/Panosse

