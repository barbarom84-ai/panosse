# Panosse v1.0.0 - Fichier prêt à distribuer

## Fichier disponible

**Emplacement** : `.\publish\Panosse.exe`
**Taille** : 74.46 Mo
**Version** : 1.0.0
**Type** : Fichier unique autonome (Single File)

---

## Caractéristiques

✅ **Autonome** : Contient tout le runtime .NET 8
✅ **Portable** : Aucune installation requise
✅ **Complet** : Toutes les ressources incluses
✅ **Optimisé** : Compilation Release avec compression

---

## Distribution

Vous pouvez distribuer ce fichier de plusieurs façons :

### Option 1 : Fichier direct
Partagez simplement `Panosse.exe` (74.46 Mo)

### Option 2 : Archive ZIP
```powershell
Compress-Archive -Path ".\publish\Panosse.exe" -DestinationPath ".\Panosse-v1.0.0.zip"
```
Taille après compression : ~25-30 Mo

### Option 3 : Créer un installateur (nécessite Inno Setup)

1. Installez Inno Setup : https://jrsoftware.org/isinfo.php
2. Lancez : `.\creer-installateur.ps1`
3. L'installateur sera dans `.\installer\`

---

## Hash SHA256

Pour vérifier l'intégrité du fichier :

```powershell
Get-FileHash .\publish\Panosse.exe -Algorithm SHA256
```

Partagez ce hash avec le fichier pour que les utilisateurs puissent vérifier son authenticité.

---

## Instructions pour les utilisateurs

### Installation
1. Télécharger `Panosse.exe`
2. Double-cliquer sur le fichier
3. Accepter les droits administrateur (UAC)
4. C'est tout !

### Utilisation
1. Lancer Panosse.exe (demande droits admin)
2. Si Chrome/Edge sont ouverts, les fermer
3. Cliquer sur le bouton bleu
4. Attendre la fin du nettoyage
5. Voir l'espace libéré

### Désinstallation
Supprimer simplement le fichier Panosse.exe (aucune installation système)

---

## Prérequis système

- Windows 10 ou 11 (64 bits)
- Droits administrateur
- Aucun autre prérequis (runtime inclus)

---

## Compatibilité

✅ Windows 10 (1809 et supérieur)
✅ Windows 11
✅ Windows Server 2019/2022
❌ Windows 7/8/8.1 (non supporté par .NET 8)
❌ Windows 32 bits

---

## Sécurité

### Antivirus
Les nouveaux exécutables non signés peuvent déclencher des alertes.
Solutions :
- Ajouter une exception dans l'antivirus
- Signer le fichier avec un certificat de code
- Partager le hash SHA256 pour vérification

### Droits administrateur
Nécessaires pour :
- Vider la corbeille
- Nettoyer C:\Windows\Temp
- Nettoyer les logs Windows
- Modifier le registre

---

## Tests recommandés avant distribution

- [ ] Tester sur une machine propre (sans Visual Studio)
- [ ] Vérifier toutes les fonctionnalités
- [ ] Scanner avec plusieurs antivirus
- [ ] Tester sur Windows 10 et Windows 11
- [ ] Vérifier la demande UAC
- [ ] Mesurer l'espace réellement libéré

---

## Support

Pour les questions ou problèmes :
- Consulter README.md
- Consulter PUBLICATION.md
- Consulter INNO-SETUP-GUIDE.md

---

## Prochaines étapes

Si vous voulez créer un installateur professionnel :

1. Installer Inno Setup (gratuit)
   https://jrsoftware.org/isinfo.php

2. Lancer le script
   ```powershell
   .\creer-installateur.ps1
   ```

3. L'installateur sera dans `.\installer\Panosse-Setup-v1.0.0.exe`

Avantages de l'installateur :
- Interface d'installation guidée
- Raccourcis automatiques (bureau + menu)
- Désinstalleur dans "Programmes et fonctionnalités"
- Plus professionnel pour la distribution

---

**Votre application Panosse est prête à être distribuée ! 🎉**

Le fichier `.\publish\Panosse.exe` peut être partagé immédiatement.

