# 🚀 Création de l'installateur Panosse v2.0.0

## 📋 Prérequis

### 1. Compilation du projet
```powershell
.\publier-v2.0.ps1
```

Cela génère `Panosse.exe` dans :
```
bin\Release\net8.0-windows\win-x64\publish\
```

### 2. Vérification des fichiers

Assurez-vous que tous ces fichiers existent :
- ✅ `bin\Release\net8.0-windows\win-x64\publish\Panosse.exe`
- ✅ `assets\panosse.ico`
- ✅ `assets\panosse_propre.ico`
- ✅ `assets\panosse_sale.ico`
- ✅ `assets\panosse.png`
- ✅ `README.md`

---

## 🛠️ Création de l'installateur

### Méthode 1 : Script automatisé (Recommandé)

```powershell
.\creer-installateur.ps1
```

Ce script effectue automatiquement :
1. Vérification d'Inno Setup
2. Compilation du script `.iss`
3. Création du fichier `installer\Panosse-Setup-v2.0.0.exe`

---

### Méthode 2 : Ligne de commande manuelle

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Panosse-Setup.iss
```

---

### Méthode 3 : Interface graphique Inno Setup

1. Ouvrez **Inno Setup Compiler**
2. Fichier → Ouvrir → Sélectionnez `Panosse-Setup.iss`
3. Build → Compile
4. L'installateur est créé dans le dossier `installer\`

---

## 🎯 Nouveautés de l'installateur v2.0.0

### ✨ Fichiers inclus

L'installateur copie maintenant :
- ✅ **Panosse.exe** (exécutable principal single-file)
- ✅ **panosse_propre.ico** (icône "propre" pour System Tray)
- ✅ **panosse_sale.ico** (icône "sale" pour alertes)
- ✅ **panosse.ico** (icône principale)
- ✅ **panosse.png** (ressource graphique)
- ✅ **README.md** (documentation)

### 🔧 Options d'installation

L'utilisateur peut choisir :

#### 1. **Icône Bureau** (cochée par défaut)
Crée un raccourci sur le bureau avec l'icône `panosse_propre.ico`

#### 2. **Icône Barre de lancement rapide** (décochée par défaut)
Crée un raccourci dans la barre des tâches

#### 3. **🆕 Lancer au démarrage de Windows** (cochée par défaut)
**NOUVELLE OPTION CRUCIALE** pour v2.0.0 !

Cette option :
- ✅ Ajoute une clé de registre dans `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- ✅ Lance Panosse automatiquement au démarrage de Windows
- ✅ Garantit que le **raccourci global Ctrl+Alt+P** est toujours actif
- ✅ Permet à l'icône System Tray de surveiller en permanence
- ✅ Active la **Mémoire Sélective** (surveillance du dossier Téléchargements)

**Clé de registre créée** :
```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
Nom : Panosse
Valeur : "C:\Program Files\Panosse\Panosse.exe"
```

**Suppression automatique** :
La clé est automatiquement supprimée lors de la désinstallation.

---

## 📦 Résultat

### Fichier créé
```
installer\Panosse-Setup-v2.0.0.exe
```

### Taille approximative
~75 Mo (exécutable + runtime .NET 8.0 + ressources)

### Informations
- **Nom** : Panosse
- **Version** : 2.0.0
- **Éditeur** : Panosse
- **URL** : https://github.com/barbarom84-ai/panosse
- **Droits** : Administrateur (requis pour nettoyage système)

---

## ✅ Tester l'installateur

### 1. Exécuter l'installateur
```powershell
.\installer\Panosse-Setup-v2.0.0.exe
```

### 2. Vérifications après installation

#### Fichiers installés
```
C:\Program Files\Panosse\
├── Panosse.exe
├── panosse.ico
├── panosse_propre.ico
├── panosse_sale.ico
├── panosse.png
└── LisezMoi.txt
```

#### Raccourcis créés
- ✅ Bureau : `Panosse` (avec icône propre)
- ✅ Menu Démarrer : `Panosse`
- ✅ Désinstallation : Menu Démarrer → "Désinstaller Panosse"

#### Clé de registre (si option cochée)
Vérifier avec :
```powershell
Get-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "Panosse"
```

Résultat attendu :
```
Panosse : "C:\Program Files\Panosse\Panosse.exe"
```

#### Fonctionnalités v2.0.0 à tester
1. **Lancer Panosse** → Doit apparaître dans le System Tray avec icône propre
2. **Fermer la fenêtre** → L'application reste en tâche de fond
3. **Appuyer sur Ctrl+Alt+P** → Nettoyage silencieux + notification Toast
4. **Redémarrer Windows** → Panosse doit se lancer automatiquement (si option cochée)
5. **Attendre 1 heure** → L'icône peut devenir rouge si Téléchargements > 5 Go

---

## 📝 Message après installation

L'utilisateur voit ce message :

```
Panosse v2.0.0 a été installé avec succès !

NOUVEAUTÉS v2.0.0 :
  - Raccourci global Ctrl+Alt+P pour nettoyer en arrière-plan
  - Icône System Tray avec changement d'état (propre/sale)
  - Surveillance intelligente du dossier Téléchargements

TIP : Si vous avez coché "Lancer au démarrage", le raccourci
Ctrl+Alt+P sera toujours disponible en arrière-plan !
```

---

## 🔄 Désinstallation

### Via le Menu Démarrer
1. Menu Démarrer → Rechercher "Panosse"
2. Clic droit → Désinstaller

### Via Paramètres Windows
1. Paramètres → Applications
2. Rechercher "Panosse"
3. Désinstaller

### Éléments supprimés automatiquement
- ✅ Tous les fichiers dans `C:\Program Files\Panosse\`
- ✅ Raccourcis (Bureau, Menu Démarrer)
- ✅ Clé de registre `Run` (lancement au démarrage)

---

## 🆘 Dépannage

### Erreur "Inno Setup non trouvé"
```powershell
# Télécharger et installer Inno Setup
Start-Process "https://jrsoftware.org/isdl.php"
```

### Erreur "Source file not found"
Vérifiez que vous avez bien compilé le projet avant :
```powershell
.\publier-v2.0.ps1
```

### Erreur "Access Denied" lors de l'installation
L'installateur nécessite les droits administrateur. Clic droit → "Exécuter en tant qu'administrateur"

---

## 📊 Comparaison avec v1.x

| Fonctionnalité | v1.x | v2.0.0 |
|---|---|---|
| Exécutable principal | ✅ | ✅ |
| Icônes multiples | ❌ | ✅ (propre + sale) |
| Lancement au démarrage | ❌ | ✅ (optionnel) |
| System Tray permanent | ❌ | ✅ |
| Raccourci global Ctrl+Alt+P | ❌ | ✅ |
| Surveillance Téléchargements | ❌ | ✅ |
| Message post-installation | ❌ | ✅ (informatif) |

---

## 🎉 C'est prêt !

Vous pouvez maintenant distribuer :
```
installer\Panosse-Setup-v2.0.0.exe
```

Cet installateur professionnel :
- ✅ Installe tous les fichiers nécessaires
- ✅ Crée les raccourcis avec les bonnes icônes
- ✅ Configure le lancement automatique (optionnel)
- ✅ Demande les droits administrateur
- ✅ Propose de lancer Panosse après installation
- ✅ Affiche un message informatif sur les nouveautés
- ✅ Désinstalle proprement tous les éléments

**Bon courage pour la distribution ! 🧹✨**

