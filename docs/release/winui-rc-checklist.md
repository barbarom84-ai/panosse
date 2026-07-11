# Panosse WinUI - Release Candidate Checklist

Cette checklist valide la pre-production WinUI avant publication.

## 1) Build et artefacts

- [ ] `dotnet build Panosse.slnx -c Release` reussit sans erreur.
- [ ] `src/Panosse.WinUI` compile en `x64` release.
- [ ] `src/Panosse.Core` compile en release.
- [ ] Binaries WinUI generes dans `src/Panosse.WinUI/bin/Release`.

## 2) Smoke tests fonctionnels WinUI

- [ ] Lancement WinUI sans crash.
- [ ] Bouton "Passer la panosse" execute un nettoyage.
- [ ] Progression et messages d'etapes s'affichent.
- [ ] Mode previsualisation ne supprime pas de fichiers.
- [ ] Exclusions sont prises en compte.
- [ ] Sauvegarde des parametres persistante au redemarrage.
- [ ] Historique recent affiche des entrees apres execution.

## 3) Update flow WinUI

- [ ] "Verifier les mises a jour" retourne un etat clair.
- [ ] "Preparer la mise a jour" telecharge et prepare le script.
- [ ] "Installer maintenant" lance le script quand disponible.
- [ ] Erreurs reseau/validation signature visibles et non bloquantes.

## 4) Integrations OS

- [ ] Hotkey global `Ctrl+Alt+P` declenche le nettoyage.
- [ ] System tray visible.
- [ ] Menu tray: Ouvrir, Passer la panosse, Quitter.
- [ ] Double-clic tray ramene la fenetre au premier plan.
- [ ] Fermeture app libere proprement hotkey + tray (pas d'icone fantome).

## 5) Qualite et observabilite

- [ ] Logs applicatifs ecrits dans `%AppData%\\Panosse\\panosse.log`.
- [ ] Telemetry counters maj apres operations.
- [ ] Aucun warning critique a la build release.
- [ ] Aucun lint error sur les fichiers modifies.

## 6) Decision GO / NO-GO

### GO si

- Build release OK.
- Tous les smoke tests critiques OK.
- Update flow prepare/install valide au moins une fois.
- Hotkey + tray stables sur 2 redemarrages consecutifs.

### NO-GO si

- Crash au lancement ou pendant nettoyage/update.
- System tray non fiable (icone fantome, menu KO).
- Hotkey instable ou non desenregistree.
- Echec de preparation update sans message utile.
