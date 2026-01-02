# Guide : Créer un VRAI installateur pour Panosse

## Problème actuel
Vous avez une application PORTABLE qui se lance directement.
Vous voulez un INSTALLATEUR qui installe l'application.

## Solution : Créer un installateur avec Inno Setup

### Étape 1 : Télécharger Inno Setup
1. Aller sur : https://jrsoftware.org/isdl.php
2. Télécharger : **Inno Setup 6.x** (gratuit)
3. Installer : Suivre l'assistant (installation standard)

### Étape 2 : Créer l'installateur
Une fois Inno Setup installé, lancez :

```powershell
.\creer-installateur.ps1
```

### Étape 3 : Distribuer l'installateur
Le fichier créé sera : `.\installer\Panosse-Setup-v1.0.0.exe`

---

## Différences

| Caractéristique | Panosse.exe (portable) | Panosse-Setup.exe (installateur) |
|-----------------|------------------------|-----------------------------------|
| **Installation** | Aucune | Assistant d'installation |
| **Emplacement** | N'importe où | C:\Program Files\Panosse\ |
| **Raccourcis** | Non | Oui (bureau + menu) |
| **Désinstallation** | Supprimer le fichier | Via Windows |
| **Taille** | 74 Mo | 75-80 Mo |
| **Professionnalisme** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |

---

## Que faire maintenant ?

### Si vous voulez garder le portable :
✅ C'est déjà prêt !
- Distribuez `.\publish\Panosse.exe`
- Les utilisateurs double-cliquent et ça marche

### Si vous voulez créer un installateur :
1. Installez Inno Setup : https://jrsoftware.org/isdl.php
2. Lancez : `.\creer-installateur.ps1`
3. Distribuez : `.\installer\Panosse-Setup-v1.0.0.exe`

---

## Recommandation

Pour une **distribution professionnelle**, je recommande l'installateur :
- Interface standard Windows
- Raccourcis automatiques
- Désinstallation propre
- Plus rassurant pour les utilisateurs

Mais le fichier portable fonctionne très bien aussi ! 😊

