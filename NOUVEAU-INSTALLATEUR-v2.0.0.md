# 🎉 Nouveau script Inno Setup pour Panosse v2.0.0

## ⚡ Résumé rapide

### Fichier créé
```
Panosse-Setup.iss (script Inno Setup complet)
```

### Version
```
2.0.0
```

---

## 🆕 Principales nouveautés

### 1. **Option "Lancer au démarrage de Windows"** ⭐
- ✅ **Cochée par défaut** lors de l'installation
- ✅ Crée une clé de registre `HKCU\...\Run`
- ✅ **Garantit que Ctrl+Alt+P est toujours actif**
- ✅ Panosse démarre en arrière-plan avec Windows
- ✅ Surveillance automatique du dossier Téléchargements
- ✅ Suppression automatique lors de la désinstallation

**Clé créée** :
```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
└── Panosse = "C:\Program Files\Panosse\Panosse.exe"
```

---

### 2. **Fichiers icônes multiples**
L'installateur copie maintenant **4 fichiers icônes** :
- `panosse.ico` (principale)
- `panosse_propre.ico` (System Tray - état propre) ⭐
- `panosse_sale.ico` (System Tray - état sale/alerte) ⭐
- `panosse.png` (ressource graphique)

**Pourquoi ?** La fonctionnalité "Mémoire Sélective" change l'icône du System Tray dynamiquement !

---

### 3. **Raccourcis avec icône "propre"**
Tous les raccourcis utilisent `panosse_propre.ico` :
- Bureau
- Menu Démarrer
- Barre de lancement rapide

---

### 4. **Message informatif post-installation**
Affiche un récapitulatif des nouveautés v2.0.0 :
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
Mentionne les nouveautés v2.0.0 dès l'écran de bienvenue.

---

## 📋 Options d'installation

| Option | Défaut | Description |
|--------|--------|-------------|
| Icône Bureau | ✅ | Raccourci sur le bureau |
| Barre lancement | ❌ | Raccourci barre des tâches |
| **🆕 Lancer au démarrage** | ✅ | **Lance Panosse avec Windows (RECOMMANDÉ)** |

---

## 🚀 Utilisation

### 1. Compiler Panosse
```powershell
.\publier-v2.0.ps1
```

### 2. Créer l'installateur
```powershell
.\creer-installateur.ps1
```

### 3. Résultat
```
installer\Panosse-Setup-v2.0.0.exe (~75 Mo)
```

---

## 📦 Contenu installé

```
C:\Program Files\Panosse\
├── Panosse.exe
├── panosse.ico
├── panosse_propre.ico ⭐
├── panosse_sale.ico ⭐
├── panosse.png
└── LisezMoi.txt
```

**+ Clé de registre** (si option cochée) :
```
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Panosse
```

---

## 🎯 Avantages du lancement au démarrage

### Pour les utilisateurs avancés ⭐
✅ **Ctrl+Alt+P disponible 24/7** (même sans ouvrir la fenêtre)
✅ **System Tray permanent** (icône toujours visible)
✅ **Surveillance automatique** (Téléchargements)
✅ **Notifications instantanées** (Toast)
✅ **Nettoyage en arrière-plan** (sans interruption)

### Scénario type
1. Windows démarre → Panosse se lance en arrière-plan
2. Icône "propre" apparaît dans le System Tray
3. Utilisateur travaille normalement
4. **Ctrl+Alt+P** → Nettoyage instantané + notification
5. Si Téléchargements > 5 Go → Icône devient "sale" (alerte)
6. Clic sur icône → Menu contextuel avec options

---

## 🔄 Désinstallation propre

L'installateur supprime automatiquement :
- ✅ Tous les fichiers dans `C:\Program Files\Panosse\`
- ✅ Tous les raccourcis (Bureau, Menu Démarrer)
- ✅ La clé de registre `Run` (lancement au démarrage)

**Aucun résidu !**

---

## 📚 Documentation complète

Pour plus de détails, consultez :
- **`CREER-INSTALLATEUR-v2.0.md`** : Guide complet de création
- **`INSTALLATEUR-v2.0-CREE.md`** : Récapitulatif détaillé
- **`Panosse-Setup.iss`** : Script Inno Setup source

---

## ✅ Checklist avant distribution

- [ ] Compiler le projet : `.\publier-v2.0.ps1`
- [ ] Vérifier `Panosse.exe` existe dans `bin\Release\...\publish\`
- [ ] Vérifier les 4 fichiers icônes dans `assets\`
- [ ] Créer l'installateur : `.\creer-installateur.ps1`
- [ ] Tester l'installation sur une machine propre
- [ ] Vérifier le lancement au démarrage (redémarrer Windows)
- [ ] Tester **Ctrl+Alt+P** (nettoyage + notification)
- [ ] Vérifier l'icône System Tray (propre/sale)
- [ ] Tester la désinstallation (vérifier suppression clé registre)

---

## 🎉 C'est prêt !

Le script Inno Setup pour Panosse v2.0.0 est **complet et prêt à l'emploi** !

**Nouveauté majeure** : L'option "Lancer au démarrage" garantit que le raccourci **Ctrl+Alt+P** et la surveillance intelligente sont toujours actifs en arrière-plan ! 🧹✨

---

**Bon courage pour la distribution ! 🚀**

