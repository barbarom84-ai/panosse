# ✅ Script Inno Setup pour Panosse v2.0.0 créé !

## 📄 Fichier généré : `Panosse-Setup.iss`

---

## 🎯 Configuration de l'installateur

### Informations générales
```ini
Nom de l'application : Panosse
Version : 2.0.0
Éditeur : Panosse
URL : https://github.com/barbarom84-ai/panosse
Droits requis : Administrateur
```

### Source des fichiers
```
Exécutable : bin\Release\net8.0-windows\win-x64\publish\Panosse.exe
Icônes :
  - assets\panosse.ico (principale)
  - assets\panosse_propre.ico (System Tray - état propre)
  - assets\panosse_sale.ico (System Tray - état sale/alerte)
  - assets\panosse.png (ressource graphique)
Documentation : README.md
```

### Dossier d'installation
```
C:\Program Files\Panosse\
```

### Fichier de sortie
```
installer\Panosse-Setup-v2.0.0.exe
```

---

## 🆕 NOUVEAUTÉS v2.0.0

### 1. **Fichiers icônes multiples**
L'installateur copie maintenant **3 fichiers icônes** :
- `panosse.ico` : Icône principale de l'application
- `panosse_propre.ico` : Pour le System Tray quand le PC est propre
- `panosse_sale.ico` : Pour le System Tray quand les Téléchargements sont encombrés

**Pourquoi ?** La fonctionnalité "Mémoire Sélective" de v2.0.0 change dynamiquement l'icône du System Tray pour alerter visuellement l'utilisateur.

---

### 2. **Option "Lancer au démarrage de Windows"** ⭐

**LA NOUVEAUTÉ MAJEURE !**

Une nouvelle option cochée par défaut permet de lancer Panosse automatiquement au démarrage de Windows.

#### Configuration technique
```ini
[Tasks]
Name: "startupicon"; 
Description: "Lancer Panosse au démarrage de Windows (recommandé pour Ctrl+Alt+P)";
GroupDescription: "Icônes supplémentaires:"

[Registry]
Root: HKCU; 
Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; 
ValueType: string; 
ValueName: "Panosse"; 
ValueData: """{app}\Panosse.exe"""; 
Flags: uninsdeletevalue; 
Tasks: startupicon
```

#### Clé de registre créée
```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
  └── Panosse = "C:\Program Files\Panosse\Panosse.exe"
```

#### Avantages
✅ **Raccourci global Ctrl+Alt+P toujours actif** (même sans ouvrir la fenêtre)
✅ **System Tray permanent** (icône visible en permanence)
✅ **Surveillance automatique** du dossier Téléchargements
✅ **Nettoyage instantané** avec le raccourci clavier depuis n'importe quelle application
✅ **Notifications Toast** en temps réel

#### Suppression automatique
La clé de registre est **automatiquement supprimée** lors de la désinstallation de Panosse. Aucun résidu !

---

### 3. **Raccourcis avec icône "propre"**

Tous les raccourcis (Bureau, Menu Démarrer) utilisent maintenant l'icône `panosse_propre.ico` pour un visuel cohérent.

```ini
[Icons]
Name: "{autodesktop}\Panosse"; 
Filename: "{app}\Panosse.exe"; 
IconFilename: "{app}\panosse_propre.ico"; 
Tasks: desktopicon
```

---

### 4. **Message informatif post-installation**

Après installation, l'utilisateur voit un message récapitulatif :

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

### 5. **Messages d'accueil mis à jour**

L'écran de bienvenue mentionne les nouveautés de v2.0.0 :

**Français** :
```
Panosse est une application de nettoyage automatique qui vous aide 
à garder votre PC propre et rapide.

NOUVEAUTÉS v2.0.0 :
  - Raccourci global Ctrl+Alt+P
  - Icône System Tray intelligente
  - Surveillance automatique des Téléchargements
```

**Anglais** :
```
Panosse is an automatic cleaning application that helps keep your 
PC clean and fast.

NEW in v2.0.0:
  - Global hotkey Ctrl+Alt+P
  - Smart System Tray icon
  - Automatic Downloads monitoring
```

---

## 📋 Options d'installation

L'utilisateur peut cocher/décocher :

| Option | Par défaut | Description |
|--------|------------|-------------|
| **Icône Bureau** | ✅ Cochée | Crée un raccourci sur le bureau |
| **Barre de lancement** | ❌ Décochée | Crée un raccourci dans la barre des tâches |
| **🆕 Lancer au démarrage** | ✅ Cochée | Ajoute Panosse au démarrage de Windows (RECOMMANDÉ) |

---

## 🔧 Détails techniques

### Compression
```ini
Compression: lzma2/max
SolidCompression: yes
```
Taille finale attendue : **~75 Mo** (exécutable + runtime .NET 8.0)

### Architecture
```ini
ArchitecturesAllowed: x64
ArchitecturesInstallIn64BitMode: x64
```
Compatible uniquement avec **Windows 64-bit**

### Langues supportées
- 🇫🇷 Français (par défaut)
- 🇬🇧 Anglais

### Vérification avant installation
Le script vérifie si Panosse est déjà en cours d'exécution et propose de le fermer automatiquement.

```pascal
if CheckForMutexes('PanosseAppMutex') then
begin
  if MsgBox('Panosse est actuellement en cours d''exécution. 
             Voulez-vous le fermer et continuer l''installation ?', 
             mbConfirmation, MB_YESNO) = IDYES then
  begin
    Exec('taskkill.exe', '/F /IM Panosse.exe', '', SW_HIDE, 
         ewWaitUntilTerminated, ResultCode);
  end;
end;
```

---

## 🚀 Comment créer l'installateur ?

### 1. Compiler le projet
```powershell
.\publier-v2.0.ps1
```

### 2. Créer l'installateur
```powershell
.\creer-installateur.ps1
```

### 3. Résultat
```
installer\Panosse-Setup-v2.0.0.exe
```

---

## 📦 Contenu du package installé

```
C:\Program Files\Panosse\
├── Panosse.exe               (exécutable principal, ~75 Mo)
├── panosse.ico               (icône principale)
├── panosse_propre.ico        (icône System Tray "propre")
├── panosse_sale.ico          (icône System Tray "sale")
├── panosse.png               (ressource graphique)
└── LisezMoi.txt              (README)
```

---

## ✅ Avantages de ce setup

### Pour l'utilisateur final
✅ **Installation en un clic** (exécuter le .exe)
✅ **Raccourcis automatiques** (Bureau, Menu Démarrer)
✅ **Désinstallation propre** (via Paramètres Windows)
✅ **Lancement au démarrage** (optionnel mais recommandé)
✅ **Pas de configuration manuelle**

### Pour le développeur
✅ **Distribution facile** (un seul fichier .exe)
✅ **Professionnel** (interface moderne Inno Setup)
✅ **Droits administrateur** (gérés automatiquement)
✅ **Mise à jour simple** (changer la version dans le script)

---

## 🎯 Cas d'usage du lancement au démarrage

### Scénario 1 : Utilisateur avancé
✅ **Coche "Lancer au démarrage"**
- Panosse démarre avec Windows (icône dans System Tray)
- **Ctrl+Alt+P** disponible 24/7 pour nettoyer instantanément
- Surveillance passive du dossier Téléchargements
- Icône change de couleur si encombrement détecté

### Scénario 2 : Utilisateur occasionnel
❌ **Décoche "Lancer au démarrage"**
- Panosse ne démarre pas automatiquement
- L'utilisateur lance manuellement depuis le raccourci Bureau
- **Ctrl+Alt+P** fonctionne uniquement quand Panosse est ouvert
- Pas de surveillance passive

**Recommandation** : Laisser coché pour profiter pleinement des fonctionnalités v2.0.0 !

---

## 📊 Comparaison des versions d'installateur

| Fonctionnalité | v1.x | v2.0.0 |
|---|---|---|
| Fichier exécutable | ✅ | ✅ |
| Icône principale | ✅ | ✅ |
| Icônes multiples | ❌ | ✅ (3 icônes) |
| Raccourcis Bureau/Menu | ✅ | ✅ |
| Lancement au démarrage | ❌ | ✅ (optionnel) |
| Clé de registre Run | ❌ | ✅ (si option cochée) |
| Message post-install | ❌ | ✅ (informatif) |
| Messages accueil v2.0 | ❌ | ✅ |
| Vérification processus | ✅ | ✅ |
| Désinstallation propre | ✅ | ✅ |

---

## 🎉 Conclusion

Le script Inno Setup pour Panosse v2.0.0 est **prêt à l'emploi** !

### Fichiers créés
- ✅ `Panosse-Setup.iss` (script Inno Setup complet)
- ✅ `CREER-INSTALLATEUR-v2.0.md` (guide détaillé)
- ✅ `INSTALLATEUR-v2.0-CREE.md` (ce document)

### Prochaines étapes
1. Compiler le projet : `.\publier-v2.0.ps1`
2. Créer l'installateur : `.\creer-installateur.ps1`
3. Tester l'installation
4. Distribuer `Panosse-Setup-v2.0.0.exe`

**Bon courage pour la distribution ! 🧹✨**

---

## 📞 Support

Pour toute question sur l'installateur :
1. Consultez `CREER-INSTALLATEUR-v2.0.md`
2. Vérifiez la documentation Inno Setup : https://jrsoftware.org/ishelp/
3. Testez l'installation sur une machine propre

