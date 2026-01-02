# 🔄 Système de mise à jour automatique

## ✅ Fonctionnalité ajoutée

Panosse vérifie maintenant automatiquement les mises à jour au démarrage !

---

## 🎯 Comment ça fonctionne

### 1. Vérification automatique

Au lancement de l'application, Panosse :
- ✅ Se connecte à l'API GitHub (en arrière-plan)
- ✅ Récupère les informations de la dernière release
- ✅ Compare la version distante avec la version locale
- ✅ Affiche une notification si une mise à jour est disponible

### 2. Notification discrète

Si une nouvelle version existe :
- 🔔 Une barre verte apparaît **en haut de la fenêtre**
- 💬 Message : "Une nouvelle version (vX.X.X) est disponible !"
- 🔘 Bouton **"Mettre à jour"** pour télécharger
- ❌ Bouton de fermeture pour masquer la notification

### 3. Téléchargement

Quand l'utilisateur clique sur **"Mettre à jour"** :
- 🌐 Ouvre la page GitHub de la release dans le navigateur
- 📥 L'utilisateur peut télécharger la nouvelle version
- 🔄 Installation manuelle (ou automatique si vous ajoutez un installateur)

---

## 🔧 Configuration

### Variables importantes dans `MainWindow.xaml.cs`

```csharp
// Version actuelle de l'application
private const string VERSION_ACTUELLE = "1.0.0";

// Nom du dépôt GitHub (format: utilisateur/repo)
private const string GITHUB_REPO = "barbarom84-ai/panosse";
```

**⚠️ IMPORTANT** : Mettez à jour `VERSION_ACTUELLE` à chaque nouvelle release !

---

## 📋 Workflow complet

### Scénario : Publier la version 1.0.1

1. **Modifier le code**
   - Faire vos changements dans Panosse
   - Corriger des bugs, ajouter des fonctionnalités

2. **Mettre à jour la version**
   ```csharp
   // Dans MainWindow.xaml.cs
   private const string VERSION_ACTUELLE = "1.0.1"; // ← Changer ici
   ```

3. **Mettre à jour le XAML (optionnel)**
   ```xml
   <!-- Dans MainWindow.xaml, panneau À propos -->
   <TextBlock Text="v1.0.1" ... />
   ```

4. **Commiter les changements**
   ```powershell
   git add .
   git commit -m "Version 1.0.1 - Corrections et améliorations"
   git push
   ```

5. **Créer la release automatique**
   ```powershell
   .\release.ps1 -Version "1.0.1"
   ```

6. **Attendre ~5 minutes**
   - GitHub Actions compile
   - Release créée avec l'exécutable

7. **Les utilisateurs sont notifiés !**
   - Au prochain lancement de Panosse
   - La barre verte apparaît automatiquement

---

## 🎨 Personnalisation

### Changer la couleur de la barre

Dans `MainWindow.xaml`, ligne ~113 :

```xml
<Border x:Name="UpdateBar"
        Background="#4CAF50"  ← Vert par défaut
        ...>
```

**Suggestions** :
- `#2196F3` - Bleu
- `#FF9800` - Orange
- `#9C27B0` - Violet
- `#F44336` - Rouge

### Changer le message

Dans `MainWindow.xaml.cs`, méthode `VerifierMiseAJour()` :

```csharp
UpdateMessage.Text = $"Une nouvelle version ({tagName}) est disponible !";
```

**Exemples** :
- `"🎉 Mise à jour disponible : {tagName}"`
- `"Nouvelle version : {tagName} - Cliquez pour télécharger"`
- `"Version {tagName} disponible !"`

### Désactiver la vérification

Dans `MainWindow.xaml.cs`, commentez cette ligne dans `MainWindow_Loaded` :

```csharp
// _ = VerifierMiseAJour(); // ← Commenter pour désactiver
```

---

## 🔍 API GitHub utilisée

### Endpoint

```
https://api.github.com/repos/{OWNER}/{REPO}/releases/latest
```

**Pour Panosse** :
```
https://api.github.com/repos/barbarom84-ai/panosse/releases/latest
```

### Réponse JSON (simplifié)

```json
{
  "tag_name": "v1.0.1",
  "name": "Panosse v1.0.1",
  "html_url": "https://github.com/barbarom84-ai/panosse/releases/tag/v1.0.1",
  "published_at": "2024-01-15T10:30:00Z",
  "assets": [
    {
      "name": "Panosse-v1.0.1.exe",
      "browser_download_url": "https://github.com/.../Panosse-v1.0.1.exe"
    }
  ]
}
```

### Limites de l'API

- **60 requêtes/heure** sans authentification
- **5000 requêtes/heure** avec authentification
- Pour Panosse : 1 requête par lancement = largement suffisant

---

## 🛡️ Sécurité et gestion des erreurs

### Erreurs gérées silencieusement

- ❌ Pas de connexion Internet → Pas de notification
- ❌ GitHub API indisponible → Pas de notification
- ❌ Timeout de la requête → Pas de notification
- ❌ JSON invalide → Pas de notification

**Principe** : Si la vérification échoue, l'application continue normalement sans alerter l'utilisateur.

### Connexion HTTPS

- ✅ Utilise HTTPS (sécurisé)
- ✅ Pas de données sensibles envoyées
- ✅ Lecture seule (GET request)

---

## 📊 Comparaison de versions

### Format supporté : Semantic Versioning

```
MAJOR.MINOR.PATCH[-SUFFIX]

Exemples :
  1.0.0
  1.2.3
  2.0.0-beta
  1.5.1-rc1
```

### Logique de comparaison

1. **MAJOR** : Si différent, le plus grand gagne
2. **MINOR** : Si MAJOR égal, compare MINOR
3. **PATCH** : Si MAJOR et MINOR égaux, compare PATCH

**Exemples** :
- `1.0.1` > `1.0.0` ✅
- `1.1.0` > `1.0.9` ✅
- `2.0.0` > `1.9.9` ✅
- `1.0.0-beta` = `1.0.0` (suffixe ignoré)

---

## 🎬 Animation de la barre

### Slide-in + Fade-in

```csharp
// La barre "glisse" de haut en bas
ThicknessAnimation (Margin: -40 → 0)

// Et apparaît en fondu
DoubleAnimation (Opacity: 0 → 1)

// Durée : 0.4 seconde
// Easing : QuadraticEase (naturel)
```

### Slide-out + Fade-out

```csharp
// Inverse du slide-in
ThicknessAnimation (Margin: 0 → -40)

// Disparaît en fondu
DoubleAnimation (Opacity: 1 → 0)

// Durée : 0.3 seconde
```

---

## 🧪 Tester la fonctionnalité

### Option 1 : Créer une fausse release

1. Créez une release `v1.0.1` sur GitHub (vide)
2. Lancez Panosse (encore en v1.0.0)
3. La barre devrait apparaître

### Option 2 : Modifier la version locale

Dans `MainWindow.xaml.cs`, changez temporairement :

```csharp
private const string VERSION_ACTUELLE = "0.9.0"; // Version antérieure
```

Relancez Panosse → La barre apparaît (si une release existe)

### Option 3 : Forcer l'affichage (debug)

Ajoutez temporairement dans `MainWindow_Loaded` :

```csharp
// Pour tester l'interface
derniereVersionUrl = "https://github.com/barbarom84-ai/panosse/releases";
UpdateMessage.Text = "TEST : Une nouvelle version est disponible !";
AfficherBarreMiseAJour();
```

---

## 🚀 Améliorations futures possibles

### 1. Téléchargement automatique

Au lieu d'ouvrir le navigateur, télécharger directement le `.exe` :

```csharp
// Récupérer l'URL de l'asset depuis l'API
var downloadUrl = root.GetProperty("assets")[0]
    .GetProperty("browser_download_url").GetString();

// Télécharger avec HttpClient
var bytes = await client.GetByteArrayAsync(downloadUrl);
File.WriteAllBytes("Panosse-Update.exe", bytes);
```

### 2. Installation automatique

Après téléchargement :

```csharp
// Lancer le nouvel exécutable
Process.Start("Panosse-Update.exe");

// Fermer l'ancienne version
Application.Current.Shutdown();
```

### 3. Notes de version (Changelog)

Afficher les nouveautés dans la notification :

```csharp
string body = root.GetProperty("body").GetString();
// Afficher dans une fenêtre popup ou un TextBlock
```

### 4. Choix "Ne plus me demander"

Ajouter un checkbox pour ignorer cette version :

```csharp
Properties.Settings.Default.IgnoredVersion = tagName;
Properties.Settings.Default.Save();
```

### 5. Vérification périodique

Au lieu de vérifier uniquement au démarrage :

```csharp
var timer = new DispatcherTimer();
timer.Interval = TimeSpan.FromHours(24);
timer.Tick += async (s, e) => await VerifierMiseAJour();
timer.Start();
```

---

## 📚 Ressources

- **GitHub API - Releases** : https://docs.github.com/en/rest/releases/releases
- **Semantic Versioning** : https://semver.org/
- **HttpClient Best Practices** : https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient

---

## ✅ Checklist

Pour que la fonctionnalité fonctionne :

- [x] `VERSION_ACTUELLE` définie dans le code
- [x] `GITHUB_REPO` configuré avec votre nom d'utilisateur
- [x] Au moins 1 release créée sur GitHub
- [x] Release avec un tag au format `vX.Y.Z`
- [x] Connexion Internet disponible

---

**🎉 Votre système de mise à jour automatique est prêt !**

Les utilisateurs seront toujours informés des nouvelles versions ! 🚀

