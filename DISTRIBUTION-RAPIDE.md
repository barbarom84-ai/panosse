# 📦 Guide Rapide - Distribution de Panosse

## 🎯 Objectif
Créer un **installateur professionnel** pour distribuer Panosse facilement.

---

## ⚡ Méthode rapide (3 étapes)

### 1️⃣ Installer Inno Setup
- Télécharger : https://jrsoftware.org/isinfo.php
- Installer : Suivez l'assistant (installation standard)

### 2️⃣ Créer l'installateur
```powershell
.\creer-installateur.ps1
```

### 3️⃣ Récupérer le fichier
Le fichier est dans : `.\installer\Panosse-Setup-v1.0.0.exe` (~75-80 Mo)

**C'est tout ! 🎉**

---

## 📋 Ce que fait le script automatiquement

1. ✅ Compile l'application en mode Release
2. ✅ Crée un fichier .exe unique (Single File)
3. ✅ Génère l'installateur Inno Setup
4. ✅ Calcule le hash SHA256 pour vérification
5. ✅ Affiche toutes les informations importantes

---

## 🎁 Contenu de l'installateur

L'installateur créera automatiquement :

- 📁 Installation dans `C:\Program Files\Panosse\`
- 🖥️ Raccourci sur le bureau
- 📌 Raccourci dans le menu Démarrer
- 🗑️ Désinstalleur dans "Programmes et fonctionnalités"
- 📄 Documentation (LisezMoi.txt, Guide-Publication.txt)

---

## 🚀 Distribution

Vous pouvez distribuer le fichier de plusieurs façons :

### Option 1 : Direct
Partagez simplement `Panosse-Setup-v1.0.0.exe`

### Option 2 : Archive
Créez un ZIP avec :
- `Panosse-Setup-v1.0.0.exe`
- `SHA256.txt` (hash de vérification)
- Instructions d'installation

### Option 3 : GitHub Releases
1. Créez une release sur GitHub
2. Uploadez l'installateur
3. Ajoutez le hash SHA256 dans la description

---

## 📊 Comparaison des méthodes de distribution

| Méthode | Fichier | Taille | Installation | Désinstallation |
|---------|---------|--------|--------------|-----------------|
| **Installateur Inno** | Setup.exe | ~75-80 Mo | Assistant guidé | Propre via Windows |
| **EXE simple** | Panosse.exe | ~74 Mo | Copie manuelle | Suppression manuelle |

**Recommandation** : Utilisez l'installateur pour une distribution professionnelle ! ✨

---

## 🔐 Sécurité

### Hash SHA256
Le script génère automatiquement un hash SHA256. Partagez-le avec l'installateur :

```
Hash: [affiché par le script]
```

Les utilisateurs peuvent vérifier :
```powershell
Get-FileHash .\Panosse-Setup-v1.0.0.exe -Algorithm SHA256
```

### Signature de code (optionnel)
Pour éviter "Éditeur inconnu" :
```powershell
signtool sign /f "certificat.pfx" /p "password" /t http://timestamp.digicert.com ".\installer\Panosse-Setup-v1.0.0.exe"
```
*(Nécessite un certificat de code ~150-300€/an)*

---

## 🧪 Tests avant distribution

Checklist complète :

- [ ] Compiler avec `.\creer-installateur.ps1`
- [ ] Tester l'installation sur une machine propre
- [ ] Vérifier tous les raccourcis (bureau + menu)
- [ ] Lancer Panosse et tester toutes les fonctionnalités
- [ ] Désinstaller et vérifier le nettoyage
- [ ] Tester sur Windows 10 ET Windows 11
- [ ] Vérifier que l'UAC s'affiche correctement
- [ ] Scanner avec un antivirus (pour éviter les faux positifs)

---

## 📖 Documentation complète

Pour plus de détails :

- 📘 `INNO-SETUP-GUIDE.md` - Guide complet Inno Setup
- 📗 `PUBLICATION.md` - Guide de publication
- 📕 `README.md` - Documentation générale

---

## 🆘 Problèmes courants

### "Inno Setup n'est pas installé"
→ Installez Inno Setup depuis https://jrsoftware.org/isinfo.php

### "Source file not found"
→ Vérifiez que le dossier `publish\` contient `Panosse.exe`
→ Relancez `.\publier.ps1` si nécessaire

### "Windows Defender bloque l'installateur"
→ Normal pour un nouveau programme
→ Ajoutez une exception ou signez le fichier

### "L'installateur est trop gros"
→ Normal ! Il contient l'application + runtime .NET
→ ~75-80 Mo est la taille standard pour une appli .NET self-contained

---

## 🎨 Personnalisation

### Changer la version
Éditez `Panosse-Setup.iss`, ligne 7 :
```pascal
#define MyAppVersion "1.0.0"
```

### Ajouter une licence
1. Créez `LICENSE.txt`
2. Dans `Panosse-Setup.iss`, ligne 38 :
```pascal
LicenseFile=LICENSE.txt
```

### Modifier les messages
Éditez la section `[CustomMessages]` dans `Panosse-Setup.iss`

---

## 📈 Statistiques typiques

Après distribution, vous pouvez vous attendre à :

- **Taille installateur** : 75-80 Mo
- **Taille après installation** : ~150 Mo
- **Temps d'installation** : 10-30 secondes
- **Compatibilité** : Windows 10 & 11 (64 bits)
- **Espace libéré par Panosse** : Variable (50 Mo à plusieurs Go)

---

## 🎉 Prêt à distribuer !

Votre installateur professionnel est prêt. Vous pouvez maintenant :

1. ✅ Le partager avec vos utilisateurs
2. ✅ Le publier sur GitHub Releases
3. ✅ Le distribuer via votre site web
4. ✅ Le mettre sur Microsoft Store (après certification)

**Bonne distribution de Panosse ! 🧹✨**

---

*Pour toute question, consultez les guides détaillés ou créez une issue sur GitHub.*

