# ⚡ Aide-mémoire Git pour Panosse

## 🚀 Commandes rapides

### Alias PowerShell (pour simplifier)

Ajoutez ceci à votre profil PowerShell pour utiliser `git` directement :

```powershell
Set-Alias -Name git -Value "C:\Program Files\Git\bin\git.exe"
```

Ou utilisez le chemin complet à chaque fois.

---

## 📝 Workflow quotidien

### 1. Vérifier les modifications

```powershell
cd "C:\Users\marco\Cursor Workplace\panosse"
& "C:\Program Files\Git\bin\git.exe" status
```

### 2. Ajouter les changements

```powershell
# Tous les fichiers
& "C:\Program Files\Git\bin\git.exe" add .

# Fichier spécifique
& "C:\Program Files\Git\bin\git.exe" add MainWindow.xaml.cs
```

### 3. Commiter

```powershell
& "C:\Program Files\Git\bin\git.exe" commit -m "Votre message"
```

### 4. Pousser vers GitHub

```powershell
& "C:\Program Files\Git\bin\git.exe" push
```

---

## 🔗 Première connexion GitHub

### Une seule fois

```powershell
# Ajouter le remote
& "C:\Program Files\Git\bin\git.exe" remote add origin https://github.com/VOTRE-NOM/panosse.git

# Renommer la branche en main
& "C:\Program Files\Git\bin\git.exe" branch -M main

# Premier push
& "C:\Program Files\Git\bin\git.exe" push -u origin main
```

---

## 📂 Emplacement du projet

```
C:\Users\marco\Cursor Workplace\panosse\
```

---

## 🆘 Commandes utiles

### Voir l'historique

```powershell
& "C:\Program Files\Git\bin\git.exe" log --oneline
```

### Annuler les modifications (avant commit)

```powershell
& "C:\Program Files\Git\bin\git.exe" restore MainWindow.xaml.cs
```

### Voir les différences

```powershell
& "C:\Program Files\Git\bin\git.exe" diff
```

---

**Pour plus de détails, consultez `GIT-INITIALISE.md` !** 📖

