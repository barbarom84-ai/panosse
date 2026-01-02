# ✅ Mémoire Sélective v2.0.0 ajoutée !

## 🧠 NOUVELLE FONCTIONNALITÉ INTELLIGENTE

Panosse surveille maintenant **discrètement** votre dossier Téléchargements et vous alerte visuellement lorsqu'il devient encombré !

---

## 🎯 CONCEPT : MÉMOIRE SÉLECTIVE

Comme une vraie serpillère qui détecte les taches, Panosse **surveille automatiquement** l'encombrement de votre PC et vous **alerte visuellement** via l'icône System Tray.

### Philosophie
- **Discret** : Surveillance en arrière-plan (0% CPU)
- **Intelligent** : Détecte les vrais problèmes (gros fichiers anciens)
- **Visuel** : Feedback immédiat via l'icône
- **Non-intrusif** : Pas de popups agaçants

---

## 🔍 SURVEILLANCE EN ARRIÈRE-PLAN

### Fréquence
```
Vérification toutes les heures (3600 secondes)
Première vérification après 30 secondes (pas de ralentissement au démarrage)
```

### Dossier surveillé
```
%USERPROFILE%\Downloads
Exemple : C:\Users\Marco\Downloads
```

### Méthode
```
✅ Asynchrone (Task.Run)
✅ Pas de blocage de l'interface
✅ Très léger en ressources
✅ Timer.Elapsed (événement automatique)
```

---

## ⚠️ SEUILS D'ALERTE

### Seuil 1 : Taille totale
```
SI taille_totale > 5 Go
ALORS état = ENCOMBRÉ
```

### Seuil 2 : Gros fichiers anciens
```
SI fichier > 200 Mo ET non modifié depuis > 30 jours
ALORS état = ENCOMBRÉ
```

### Logique
```
Encombré = (Taille > 5 Go) OU (Gros fichiers anciens > 0)
```

---

## 🎨 FEEDBACK VISUEL (SYSTEM TRAY)

### État PROPRE 🟢
```
Icône : 🧹 Serpillère normale
Tooltip : "Panosse - La serpillère numérique"
Menu "Pourquoi rouge?" : MASQUÉ
```

### État ENCOMBRÉ 🔴
```
Icône : 🧹🔴 Serpillère avec point rouge
Tooltip : "⚠️ Panosse - Téléchargements encombré (X.X Go)"
Menu "Pourquoi rouge?" : VISIBLE
```

### Création de l'icône rouge
```csharp
// Copie de l'icône normale
Bitmap bitmap = new Bitmap(16, 16);
graphics.DrawIcon(iconeNormale, 0, 0);

// Ajout d'un point rouge en haut à droite
graphics.FillEllipse(redBrush, 10, 0, 6, 6);

// Conversion en icône
iconeAlerte = Icon.FromHandle(bitmap.GetHicon());
```

---

## 📋 MENU CONTEXTUEL DYNAMIQUE

### Menu normal (propre)
```
🪟 Ouvrir Panosse
🧹 Passer la panosse maintenant
───────────────────────
❌ Quitter
```

### Menu alerte (encombré)
```
🪟 Ouvrir Panosse
🧹 Passer la panosse maintenant
───────────────────────
❓ Pourquoi l'icône est rouge ?  ← NOUVEAU
───────────────────────
❌ Quitter
```

---

## 💬 BULLE D'INFORMATION

### Déclenchement
```
Clic sur "❓ Pourquoi l'icône est rouge ?"
```

### Contenu (Cas 1 : Taille ET fichiers anciens)
```
╔════════════════════════════════════════════╗
║  ⚠️ Dossier Téléchargements encombré      ║
║                                            ║
║  Votre dossier Téléchargements commence   ║
║  à être encombré:                          ║
║                                            ║
║  📦 Taille totale: 7.42 Go                ║
║  📂 3 gros fichier(s) ancien(s)           ║
║     (>200 Mo, >30 jours)                  ║
║                                            ║
║  💡 Appuyez sur Ctrl+Alt+P pour faire     ║
║     de la place !                          ║
║                                            ║
║                    [Panosse] ⏱️ il y a 0s  ║
╚════════════════════════════════════════════╝
```

### Durée
```
8 secondes (pour avoir le temps de lire)
```

### Type
```
BalloonTip Warning (icône ⚠️)
```

---

## 🔄 CYCLE DE SURVEILLANCE

### Séquence complète
```
1. ⏰ Timer déclenché (toutes les heures)
   │
2. 🔍 Analyse asynchrone du dossier Downloads
   │   ├─ Parcours de tous les fichiers
   │   ├─ Calcul de la taille totale
   │   ├─ Détection des gros fichiers anciens
   │   └─ Comparaison avec les seuils
   │
3. 📊 Mise à jour des statistiques
   │   ├─ tailleTelechargementsGo
   │   └─ nombreFichiersAnciens
   │
4. 🎨 Changement d'icône si nécessaire
   │   ├─ Si encombré : Icône rouge
   │   └─ Si propre : Icône normale
   │
5. 📋 Mise à jour du menu contextuel
   │   ├─ Afficher/masquer "Pourquoi rouge?"
   │   └─ Modifier le tooltip
   │
6. 💤 Attente d'1 heure
   │
7. 🔁 Retour à l'étape 1
```

---

## 📊 ANALYSE DÉTAILLÉE

### Parcours des fichiers
```csharp
string downloadPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
    "Downloads"
);

var fichiers = Directory.GetFiles(downloadPath, "*", SearchOption.AllDirectories);

foreach (var fichier in fichiers)
{
    var info = new FileInfo(fichier);
    
    // Taille totale
    tailleTotal += info.Length;
    
    // Fichiers anciens
    long tailleMo = info.Length / (1024 * 1024);
    if (tailleMo >= 200 && info.LastWriteTime < DateTime.Now.AddDays(-30))
    {
        fichiersAnciens++;
    }
}
```

### Gestion des erreurs
```csharp
try
{
    // Analyse du fichier
}
catch
{
    // Ignorer les fichiers inaccessibles (en cours d'utilisation, permissions)
}
```

---

## ⚙️ CONSTANTES CONFIGURABLES

### Dans le code source
```csharp
private const double SEUIL_TAILLE_GO = 5.0;          // Taille max en Go
private const long SEUIL_FICHIER_GROS_MO = 200;      // Taille fichier en Mo
private const int SEUIL_JOURS_ANCIEN = 30;           // Ancienneté en jours
```

### Personnalisation future
Pour changer les seuils, modifier ces constantes dans `MainWindow.xaml.cs`.

---

## 🚀 SCÉNARIOS D'UTILISATION

### Scénario 1 : Dossier commence à se remplir
```
👤 Utilisateur télécharge beaucoup de fichiers
   → Après plusieurs jours, dossier atteint 6 Go
   → Icône System Tray devient rouge 🔴
   → Tooltip : "⚠️ Panosse - Téléchargements encombré (6.2 Go)"
   → Utilisateur clique droit → "Pourquoi rouge?"
   → Bulle info s'affiche
   → Ctrl+Alt+P pour nettoyer
   ✅ 3 Go libérés, icône redevient normale 🟢
```

### Scénario 2 : Gros fichiers oubliés
```
👤 Utilisateur a téléchargé des ISO de 4 Go il y a 2 mois
   → Fichiers toujours là (jamais déplacés)
   → Après vérification, icône devient rouge 🔴
   → "2 gros fichier(s) ancien(s)"
   → Nettoyage Ctrl+Alt+P
   → Fichiers anciens supprimés
   ✅ Icône redevient normale 🟢
```

### Scénario 3 : Dossier bien géré
```
👤 Utilisateur déplace régulièrement ses fichiers
   → Dossier toujours < 5 Go
   → Pas de gros fichiers anciens
   → Icône reste normale 🟢
   → Menu "Pourquoi rouge?" invisible
   ✅ Aucune alerte, tout va bien
```

### Scénario 4 : Premier démarrage
```
👤 Panosse lancé pour la première fois
   → Attente de 30 secondes (pas de ralentissement)
   → Première vérification
   → Si encombré : Icône rouge immédiate
   → Sinon : Icône normale
   → Prochaine vérif dans 1 heure
   ✅ Surveillance démarrée
```

---

## 🛠️ IMPLÉMENTATION TECHNIQUE

### 1. Déclarations de propriétés
```csharp
private System.Timers.Timer? surveillanceTimer;
private bool dossierTelechargementsEncombre = false;
private double tailleTelechargementsGo = 0;
private int nombreFichiersAnciens = 0;
private Drawing.Icon? iconeNormale;
private Drawing.Icon? iconeAlerte;
```

### 2. Démarrage de la surveillance
```csharp
private void DemarrerSurveillanceTelechi()
{
    surveillanceTimer = new System.Timers.Timer(3600000); // 1 heure
    surveillanceTimer.Elapsed += async (sender, e) => 
        await VerifierEncombrementTelechi();
    surveillanceTimer.AutoReset = true;
    surveillanceTimer.Start();
    
    // Première vérification après 30 secondes
    Task.Run(async () =>
    {
        await Task.Delay(30000);
        await VerifierEncombrementTelechi();
    });
}
```

### 3. Vérification asynchrone
```csharp
private async Task VerifierEncombrementTelechi()
{
    await Task.Run(() =>
    {
        // Analyse du dossier
        // Calcul des statistiques
        // Comparaison avec seuils
        
        // Mise à jour de l'icône si changement d'état
        if (etaitEncombre != dossierTelechargementsEncombre)
        {
            Dispatcher.InvokeAsync(() => MettreAJourIconeSystemTray());
        }
    });
}
```

### 4. Changement d'icône
```csharp
private void MettreAJourIconeSystemTray()
{
    if (dossierTelechargementsEncombre)
    {
        // Icône rouge
        notifyIcon.Icon = iconeAlerte;
        notifyIcon.Text = $"⚠️ Panosse - Téléchargements encombré...";
        
        // Afficher menu "Pourquoi rouge?"
        menuPourquoi.Visible = true;
    }
    else
    {
        // Icône normale
        notifyIcon.Icon = iconeNormale;
        notifyIcon.Text = "Panosse - La serpillère numérique";
        
        // Masquer menu "Pourquoi rouge?"
        menuPourquoi.Visible = false;
    }
}
```

### 5. Affichage de l'explication
```csharp
private void AfficherExplicationEncombrement()
{
    string message = $"Votre dossier Téléchargements commence à être encombré:\n\n" +
                     $"📦 Taille totale: {tailleTelechargementsGo:F2} Go\n" +
                     $"📂 {nombreFichiersAnciens} gros fichier(s) ancien(s)\n\n" +
                     $"💡 Appuyez sur Ctrl+Alt+P pour faire de la place !";
    
    notifyIcon.ShowBalloonTip(8000, "⚠️ Dossier Téléchargements encombré", 
                               message, Forms.ToolTipIcon.Warning);
}
```

---

## 🔒 OPTIMISATIONS

### Performance
```
✅ Vérification asynchrone (Task.Run)
✅ Pas de blocage UI (Dispatcher.InvokeAsync)
✅ Ignorer fichiers inaccessibles (try-catch par fichier)
✅ SearchOption.AllDirectories (inclut sous-dossiers)
```

### Mémoire
```
✅ Pas de stockage massif de données
✅ Seulement 2 icônes en mémoire
✅ Variables simples (bool, double, int)
✅ Dispose du Timer à la fermeture
```

### CPU
```
✅ Vérification toutes les heures (pas en continu)
✅ Analyse rapide (lecture métadonnées seulement)
✅ Pas de FileSystemWatcher (trop de notifications)
✅ Timer System.Timers (thread pool)
```

---

## 📐 ARCHITECTURE

### Flux de données
```
┌──────────────────────────────────────────┐
│        MainWindow (Démarrage)            │
│  └─ InitialiserSystemTray()              │
│      └─ DemarrerSurveillanceTelechi()    │
└──────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────┐
│     Timer (Toutes les heures)            │
│  └─ Elapsed Event                        │
│      └─ VerifierEncombrementTelechi()    │
└──────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────┐
│  Analyse Asynchrone (Task.Run)           │
│  └─ Parcours fichiers Downloads          │
│  └─ Calcul taille + ancienneté           │
│  └─ Comparaison seuils                   │
└──────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────┐
│  Mise à jour état (si changement)        │
│  └─ dossierTelechargementsEncombre       │
│  └─ tailleTelechargementsGo              │
│  └─ nombreFichiersAnciens                │
└──────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────┐
│  UI Update (Dispatcher.InvokeAsync)      │
│  └─ MettreAJourIconeSystemTray()         │
│      ├─ Changer icône (rouge/normale)    │
│      ├─ Modifier tooltip                 │
│      └─ Afficher/masquer menu            │
└──────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────┐
│  Interaction Utilisateur (optionnel)     │
│  └─ Clic sur "Pourquoi rouge?"           │
│      └─ AfficherExplicationEncombrement()│
│          └─ BalloonTip (8 secondes)      │
└──────────────────────────────────────────┘
```

---

## 🎯 AVANTAGES

### Pour l'utilisateur
1. ✅ **Surveillance automatique** - Pas besoin d'y penser
2. ✅ **Feedback visuel** - Icône rouge = problème
3. ✅ **Info contextuelle** - Explication claire du problème
4. ✅ **Action suggérée** - "Appuyez sur Ctrl+Alt+P"
5. ✅ **Non-intrusif** - Pas de popups agaçants
6. ✅ **Discret** - Vérification en arrière-plan

### Pour l'application
1. ✅ **Professionnalisme** - Feature avancée
2. ✅ **Intelligence** - Détection des vrais problèmes
3. ✅ **Performance** - Très léger en ressources
4. ✅ **Évolutivité** - Facile d'ajouter d'autres surveillances

---

## 🧪 TESTS À EFFECTUER

### Test 1 : Démarrage de la surveillance
1. Lancez Panosse
2. Attendez 30 secondes
3. Vérifiez la console Debug :
   ```
   ✅ Surveillance du dossier Téléchargements démarrée
   📊 Téléchargements: 2.34 Go, 0 gros fichiers anciens
   ```

### Test 2 : Dossier encombré (taille)
1. Remplissez Downloads avec > 5 Go
2. Attendez la vérification (ou redémarrez Panosse)
3. **Résultat attendu** :
   - Icône devient rouge 🔴
   - Tooltip change
   - Menu "Pourquoi rouge?" apparaît

### Test 3 : Gros fichiers anciens
1. Mettez un fichier de 250 Mo dans Downloads
2. Changez sa date de modification (> 30 jours)
3. Redémarrez Panosse ou attendez vérification
4. **Résultat attendu** :
   - Icône rouge même si < 5 Go
   - "1 gros fichier(s) ancien(s)"

### Test 4 : Menu "Pourquoi rouge?"
1. Dossier encombré (icône rouge)
2. Clic droit sur l'icône
3. **Résultat attendu** :
   - Menu "❓ Pourquoi l'icône est rouge?" visible
   - Entre "Nettoyer" et "Quitter"

### Test 5 : Bulle d'information
1. Clic sur "Pourquoi rouge?"
2. **Résultat attendu** :
   - Bulle BalloonTip s'affiche
   - Détails : Taille + Fichiers anciens
   - Suggestion : "Ctrl+Alt+P"
   - Durée : 8 secondes

### Test 6 : Retour à la normale
1. Dossier encombré (icône rouge)
2. Nettoyez (Ctrl+Alt+P ou manuellement)
3. Attendez la vérification (ou redémarrez)
4. **Résultat attendu** :
   - Icône redevient normale 🟢
   - Menu "Pourquoi rouge?" disparaît

---

## 🎊 RÉSUMÉ

### Ce qui a été ajouté (v2.0.0)

1. ✅ **Timer de surveillance** (1 heure)
2. ✅ **Analyse asynchrone** du dossier Downloads
3. ✅ **Détection intelligente** (taille + fichiers anciens)
4. ✅ **Icône dynamique** (normale/rouge)
5. ✅ **Menu contextuel dynamique** ("Pourquoi rouge?")
6. ✅ **Bulle d'information** détaillée
7. ✅ **Optimisations performance** (async, léger)

### Fichiers modifiés

- `MainWindow.xaml.cs` :
  - Nouvelles propriétés (Timer, icônes, stats)
  - DemarrerSurveillanceTelechi()
  - VerifierEncombrementTelechi()
  - CreerIconeAlerte()
  - MettreAJourIconeSystemTray()
  - AfficherExplicationEncombrement()
  - ArreterSurveillanceTelechi()

- `Panosse.csproj` : Version 2.0.0
- `Panosse-Setup.iss` : Version 2.0.0

---

**Panosse surveille maintenant votre PC comme une vraie serpillère intelligente ! 🧠🧹✨**

