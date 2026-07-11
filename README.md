# Panosse - La serpillère numérique

Application Windows native (WinUI 3) pour nettoyer rapidement votre PC.

![.NET](https://img.shields.io/badge/.NET-10-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## Fonctionnalités

- Nettoyage complet : corbeille, temp, caches navigateurs, registre, logs, miniatures
- Mode prévisualisation (scan sans suppression)
- Exclusions intelligentes et nettoyage planifié
- Historique des opérations
- Mises à jour automatiques depuis GitHub
- Raccourci global `Ctrl+Alt+P`
- Icône dans la barre des tâches (tray)

## Structure du projet

```
panosse/
├── Panosse.slnx
├── src/
│   ├── Panosse.Core/          # ViewModels, commandes, services métier
│   └── Panosse.WinUI/         # Interface WinUI 3
├── docs/
├── _Scripts/                  # Publication et installateur
└── Panosse-Setup.iss
```

## Développement

```powershell
# Restaurer et compiler
dotnet build Panosse.slnx

# Lancer en debug
dotnet run --project src/Panosse.WinUI/Panosse.WinUI.csproj
```

## Publication

```powershell
# Binaire release
.\_Scripts\publier.ps1

# Installateur Inno Setup
.\_Scripts\creer-installateur.ps1
```

## Version

```powershell
.\_Scripts\bump-version.ps1 -NewVersion "3.0.0"
```

## Checklists release

- `docs/release/winui-rc-checklist.md`
- `docs/release/monthly-release-checklist.md`
- `docs/release/winui-v3-beta-release-notes.md`

## Prérequis

- Windows 10/11 (x64)
- .NET 10 SDK
- Droits administrateur (nettoyage système)

---

Créé par Marco Barbaro — [GitHub](https://github.com/barbarom84-ai/panosse)
