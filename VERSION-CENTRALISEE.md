# 🎯 Gestion centralisée de la version - Implémentation

## ✅ Objectif

Définir la version de l'application **à un seul endroit** pour faciliter les futures mises à jour et éviter les incohérences.

---

## 📍 Source unique de vérité : `Panosse.csproj`

### Configuration dans le fichier projet

```xml
<PropertyGroup>
  <!-- Informations de version -->
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
  <Company>Panosse</Company>
  <Product>Panosse - Nettoyeur PC</Product>
  <Copyright>Copyright © 2025</Copyright>
  <Description>Application de nettoyage automatique pour Windows</Description>
</PropertyGroup>
```

**Source unique** : La balise `<Version>1.0.0</Version>` est maintenant la seule à modifier !

---

## 🔧 Lecture automatique dans le code C#

### Avant (version codée en dur)

```csharp
// ❌ Mauvaise pratique - Version dupliquée
private const string VERSION_ACTUELLE = "1.0.0";
```

**Problèmes** :
- Duplication dans `.csproj` ET dans le code
- Risque d'oubli lors des mises à jour
- Incohérence possible

### Après (version lue depuis l'assembly)

```csharp
// ✅ Bonne pratique - Version centralisée
private static readonly string VERSION_ACTUELLE = 
    Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
```

**Avantages** :
- ✅ Une seule source de vérité (`.csproj`)
- ✅ Lecture automatique au runtime
- ✅ Pas de duplication
- ✅ Aucun risque d'incohérence

---

## 📊 Affichage dynamique dans l'interface

### XAML - TextBlock nommé

```xml
<TextBlock x:Name="VersionText"
           Text="v1.0.0"
           FontSize="14"
           Foreground="#9E9E9E"
           HorizontalAlignment="Center"
           Margin="0,0,0,20"/>
```

**Note** : Le `Text="v1.0.0"` est juste une valeur par défaut pour le designer XAML.

### C# - Initialisation dans le constructeur

```csharp
public MainWindow()
{
    InitializeComponent();
    Loaded += MainWindow_Loaded;
    TaskList.ItemsSource = taskMessages;
    
    // Définir la version dynamiquement depuis l'assembly
    VersionText.Text = $"v{VERSION_ACTUELLE}";
}
```

**Résultat** : La version affichée dans "À propos" est toujours synchronisée !

---

## 🎯 Workflow de mise à jour simplifié

### Avant (3 endroits à modifier)

```
1. Modifier Panosse.csproj → <Version>1.0.1</Version>
2. Modifier MainWindow.xaml.cs → VERSION_ACTUELLE = "1.0.1"
3. Modifier MainWindow.xaml → Text="v1.0.1"
```

**Risque d'oubli élevé !** 😰

### Maintenant (1 seul endroit)

```
1. Modifier Panosse.csproj → <Version>1.0.1</Version>
```

**C'est tout !** 🎉

Le reste se met à jour automatiquement :
- ✅ `VERSION_ACTUELLE` lit la nouvelle version
- ✅ L'interface affiche `v1.0.1`
- ✅ La vérification de mise à jour compare avec `1.0.1`
- ✅ Le fichier `.exe` a les bonnes métadonnées

---

## 📋 Processus de release complet

### Étape 1 : Modifier la version dans `.csproj`

```xml
<!-- Avant -->
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>

<!-- Après -->
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

**Optionnel** : Vous pouvez aussi modifier :
- `<Copyright>` si l'année change
- `<Description>` si les fonctionnalités évoluent

### Étape 2 : Commiter et pousser

```powershell
git add Panosse.csproj
git commit -m "Bump version to 1.0.1"
git push
```

### Étape 3 : Créer la release

```powershell
.\release-simple.ps1 -Version "1.0.1"
```

### Étape 4 : Vérifier

Une fois la release créée (~5 min), vérifiez :

1. **Sur GitHub** : https://github.com/barbarom84-ai/panosse/releases
   - Tag : `v1.0.1`
   - Asset : `Panosse-v1.0.1.exe`

2. **Dans l'application** :
   - Téléchargez `Panosse-v1.0.1.exe`
   - Lancez-le
   - Ouvrez "À propos" → Devrait afficher `v1.0.1`

3. **Métadonnées du fichier** :
   - Clic droit sur `Panosse-v1.0.1.exe` → Propriétés → Détails
   - Version du fichier : `1.0.1.0`
   - Version du produit : `1.0.1`

**Tout est synchronisé automatiquement !** ✅

---

## 🔍 Comment fonctionne `Assembly.GetExecutingAssembly()`

### Code détaillé

```csharp
private static readonly string VERSION_ACTUELLE = 
    Assembly.GetExecutingAssembly()     // Obtient l'assembly actuel (Panosse.exe)
           .GetName()                   // Obtient le nom de l'assembly
           .Version?                    // Obtient la version (nullable)
           .ToString(3)                 // Formate en "X.Y.Z" (3 composants)
           ?? "1.0.0";                  // Fallback si null
```

### Explication

1. **`Assembly.GetExecutingAssembly()`** : Retourne l'assembly en cours d'exécution (votre application compilée)

2. **`.GetName()`** : Obtient les métadonnées de l'assembly (nom, version, culture, etc.)

3. **`.Version`** : Propriété de type `Version?` qui contient :
   - `Major` : 1
   - `Minor` : 0
   - `Build` : 0
   - `Revision` : 0

4. **`.ToString(3)`** : Formate avec 3 composants seulement :
   - `ToString(3)` → `"1.0.0"` (Major.Minor.Build)
   - `ToString(4)` → `"1.0.0.0"` (Major.Minor.Build.Revision)

5. **`?? "1.0.0"`** : Valeur par défaut si `Version` est `null` (impossible normalement)

### Pourquoi `static readonly` ?

```csharp
private static readonly string VERSION_ACTUELLE = ...
```

- **`static`** : Une seule instance pour toute l'application
- **`readonly`** : Ne peut pas être modifiée après l'initialisation
- **Initialisé au chargement de la classe** : Avant même le constructeur

---

## 📊 Avantages de cette approche

### 1. Source unique de vérité

```
.csproj → Assembly → CODE → INTERFACE
   ↓         ↓         ↓        ↓
Version  Métadonnées  Logic   Display
```

Tout part du `.csproj` !

### 2. Pas de duplication

**Avant** :
- ❌ `.csproj` : `<Version>1.0.0</Version>`
- ❌ C# : `VERSION_ACTUELLE = "1.0.0"`
- ❌ XAML : `Text="v1.0.0"`

**Maintenant** :
- ✅ `.csproj` : `<Version>1.0.0</Version>`
- ✅ C# : Lit automatiquement
- ✅ XAML : Mis à jour au runtime

### 3. Métadonnées Windows cohérentes

Quand vous faites **Clic droit → Propriétés** sur `Panosse.exe` :

```
Propriétés du fichier
├─ Nom du fichier : Panosse.exe
├─ Version du fichier : 1.0.0.0      ← Depuis <FileVersion>
├─ Version du produit : 1.0.0        ← Depuis <Version>
├─ Copyright : Copyright © 2025      ← Depuis <Copyright>
├─ Nom du produit : Panosse - Nettoyeur PC  ← Depuis <Product>
└─ Description : Application de...   ← Depuis <Description>
```

Toutes les métadonnées sont cohérentes !

### 4. Facilite les tests

```csharp
// Dans vos tests unitaires (si vous en ajoutez)
var version = Assembly.GetExecutingAssembly().GetName().Version;
Assert.AreEqual(new Version(1, 0, 0), version);
```

### 5. Compatible avec les outils

- **NuGet** : Lit `<Version>` pour les packages
- **MSBuild** : Utilise `<AssemblyVersion>` pour la compilation
- **Windows** : Lit `<FileVersion>` pour les propriétés du fichier
- **CI/CD** : Peut extraire la version du `.csproj`

---

## 🎨 Autres endroits où la version est utilisée

### 1. Vérification de mise à jour

```csharp
private async Task VerifierMiseAJour()
{
    // ...
    if (EstVersionPlusRecente(versionDistante, VERSION_ACTUELLE))
    {
        // Une mise à jour est disponible
    }
}
```

**Automatiquement synchronisé !** ✅

### 2. Panneau "À propos"

```csharp
VersionText.Text = $"v{VERSION_ACTUELLE}";
```

**Affiche toujours la bonne version !** ✅

### 3. MessageBox de mise à jour

```csharp
MessageBox.Show(
    $"Version actuelle : {VERSION_ACTUELLE}\n" +
    $"Nouvelle version : {derniereVersionTag}"
);
```

**Version cohérente dans les messages !** ✅

### 4. Logs (si vous en ajoutez)

```csharp
Debug.WriteLine($"Panosse v{VERSION_ACTUELLE} démarré");
```

---

## 🧪 Comment tester

### Test 1 : Vérifier la lecture de version

1. **Compilez** l'application
2. **Lancez** Panosse
3. **Ouvrez "À propos"**
4. **Vérifiez** : Devrait afficher `v1.0.0`

### Test 2 : Modifier la version

1. **Ouvrez** `Panosse.csproj`
2. **Changez** :
   ```xml
   <Version>1.0.1</Version>
   <AssemblyVersion>1.0.1.0</AssemblyVersion>
   <FileVersion>1.0.1.0</FileVersion>
   ```
3. **Recompilez** : `dotnet build`
4. **Relancez** Panosse
5. **Ouvrez "À propos"** → Devrait afficher `v1.0.1`
6. **Vérifiez les métadonnées** : Clic droit → Propriétés → Détails

### Test 3 : Vérification de mise à jour

1. **Gardez** la version locale à `1.0.0`
2. **Créez** une release `v1.0.1` sur GitHub
3. **Lancez** Panosse v1.0.0
4. **Vérifiez** : La barre verte devrait apparaître
5. **Message** : "Une nouvelle version (v1.0.1) est disponible !"

---

## 📝 Checklist de mise à jour de version

Quand vous voulez publier une nouvelle version :

- [ ] Modifier **`Panosse.csproj`** :
  - [ ] `<Version>1.0.X</Version>`
  - [ ] `<AssemblyVersion>1.0.X.0</AssemblyVersion>`
  - [ ] `<FileVersion>1.0.X.0</FileVersion>`
  - [ ] (Optionnel) `<Copyright>` si année change

- [ ] **NE PAS MODIFIER** :
  - [ ] ❌ `MainWindow.xaml.cs` (lit automatiquement)
  - [ ] ❌ `MainWindow.xaml` (mis à jour au runtime)

- [ ] Commiter et pousser :
  ```powershell
  git add Panosse.csproj
  git commit -m "Bump version to 1.0.X"
  git push
  ```

- [ ] Créer la release :
  ```powershell
  .\release-simple.ps1 -Version "1.0.X"
  ```

- [ ] Vérifier après 5 minutes :
  - [ ] Release sur GitHub
  - [ ] Fichier téléchargeable
  - [ ] Métadonnées correctes
  - [ ] Version affichée dans "À propos"

---

## 🎁 Bonus : Script de bump de version

Vous pouvez créer un script PowerShell pour automatiser :

```powershell
# bump-version.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion  # Ex: "1.0.1"
)

# Mettre à jour le .csproj
$csprojPath = "Panosse.csproj"
$content = Get-Content $csprojPath -Raw

$content = $content -replace '<Version>[\d.]+</Version>', "<Version>$NewVersion</Version>"
$content = $content -replace '<AssemblyVersion>[\d.]+</AssemblyVersion>', "<AssemblyVersion>$NewVersion.0</AssemblyVersion>"
$content = $content -replace '<FileVersion>[\d.]+</FileVersion>', "<FileVersion>$NewVersion.0</FileVersion>"

Set-Content $csprojPath $content

Write-Host "Version mise à jour : $NewVersion" -ForegroundColor Green
```

**Usage** :
```powershell
.\bump-version.ps1 -NewVersion "1.0.1"
git add Panosse.csproj
git commit -m "Bump version to 1.0.1"
git push
.\release-simple.ps1 -Version "1.0.1"
```

---

## ✅ Résumé

### Avant

```
3 endroits à modifier manuellement
↓
Risque d'incohérence élevé
↓
Maintenance difficile
```

### Maintenant

```
1 seul endroit (.csproj)
↓
Lecture automatique partout
↓
Maintenance facile
```

**Avantages** :
- ✅ **Source unique** : `.csproj`
- ✅ **Lecture automatique** : `Assembly.GetExecutingAssembly()`
- ✅ **Affichage dynamique** : Interface synchronisée
- ✅ **Métadonnées cohérentes** : Propriétés Windows
- ✅ **Facilité de maintenance** : 1 seul changement
- ✅ **Pas de duplication** : DRY (Don't Repeat Yourself)
- ✅ **Moins d'erreurs** : Impossible d'oublier

---

**🎯 Votre version est maintenant gérée de manière centralisée et professionnelle ! 🎯**

