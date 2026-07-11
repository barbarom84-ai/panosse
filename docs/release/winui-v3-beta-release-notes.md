# Panosse v3 Beta - Release Notes

Date: 2026-03-15
Statut: Beta

## Points forts

- Nouvelle base WinUI 3 introduite en parallele de l'application WPF existante.
- Architecture refactoree autour de services et orchestrateurs reutilisables.
- Parcours nettoyage moderne avec progression en temps reel.
- Update pipeline renforce (verification, preparation, installation).
- Integrations OS ajoutees sur WinUI (hotkey globale + system tray).

## Nouveautes principales

### Interface WinUI moderne

- Nouveau shell WinUI avec ressources centralisees (styles/tokens).
- Vue principale operationnelle pour:
  - lancer le nettoyage
  - suivre la progression
  - afficher les messages d'etapes
  - consulter l'historique recent

### Nettoyage (v3)

- Execution via `CleanupOrchestrator` avec flux progressif.
- Support du mode previsualisation (sans suppression).
- Support des exclusions intelligentes (patterns).

### Parametres et historique

- Parametres persistants sur WinUI:
  - mode previsualisation
  - patterns d'exclusion
- Historique des operations visible dans l'interface WinUI.

### Mises a jour

- Verification des mises a jour depuis GitHub.
- Preparation de mise a jour avec progression de telechargement.
- Installation declenchable depuis WinUI apres preparation.
- Validation d'integrite du binaire telecharge avant installation.

### Integrations systeme (WinUI)

- Hotkey globale `Ctrl+Alt+P` pour declencher un nettoyage.
- Icône system tray avec menu:
  - Ouvrir Panosse
  - Passer la panosse
  - Quitter
- Double-clic tray pour restaurer la fenetre.

## Qualite et fiabilite

- Commandes async securisees avec gestion d'erreur.
- Instrumentation telemetry pour durées et compteurs.
- Logging structure local.
- Build solution valide en Debug et Release.

## Migration et compatibilite

- Migration progressive: WPF reste disponible pendant la montee en charge WinUI.
- Core partage pour limiter la duplication de logique metier.

## Known limitations (beta)

- Validation fonctionnelle tray/hotkey/update a confirmer sur plusieurs environnements Windows.
- UX de certains ecrans WinUI encore en phase de polish.
- Documentation utilisateur finale WinUI en cours d'alignement.

## Checklist de validation

Utiliser la checklist RC WinUI:

- `docs/release/winui-rc-checklist.md`

## Notes pour testeurs beta

Verifier en priorite:

1. Nettoyage manuel, previsualisation et exclusions.
2. Flux update complet (check -> preparation -> installation).
3. Comportement tray et hotkey apres redemarrage.
4. Stabilite generale (aucun crash, logs etats coherents).
