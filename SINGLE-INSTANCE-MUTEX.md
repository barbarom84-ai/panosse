# ✅ SINGLE INSTANCE IMPLÉMENTÉ - Panosse v2.0.0

## 🎯 OBJECTIF

Empêcher le lancement de plusieurs instances de Panosse pour :
- ✅ Éviter la confusion utilisateur (plusieurs fenêtres/icônes)
- ✅ Économiser les ressources système
- ✅ Garantir une seule icône dans le System Tray
- ✅ Message clair et explicatif

---

## 🔧 IMPLÉMENTATION

### Fichier modifié : `App.xaml.cs`

#### 1️⃣ Ajout du Mutex

```csharp
// Mutex pour empêcher plusieurs instances de Panosse
private Mutex? instanceMutex;
private const string MUTEX_NAME = "Panosse_Unique_Mutex_99";
```

**Explication** :
- `Mutex` : Objet de synchronisation système (global à Windows)
- Nom unique : `"Panosse_Unique_Mutex_99"` identifie l'application
- Si le Mutex existe déjà → Une instance de Panosse est active

---

#### 2️⃣ Vérification au démarrage (`OnStartup`)

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    // Vérifier si une instance de Panosse est déjà en cours d'exécution
    bool isNewInstance;
    instanceMutex = new Mutex(true, MUTEX_NAME, out isNewInstance);
    
    if (!isNewInstance)
    {
        // Une instance de Panosse est déjà active
        MessageBox.Show(
            "Panosse est déjà active dans la barre des tâches.\n\n" +
            "Astuce : Double-cliquez sur l'icône 🧹 dans la barre des tâches pour afficher la fenêtre.",
            "Panosse - Déjà active",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
        
        // Fermer cette nouvelle instance immédiatement
        Environment.Exit(0);
        return;
    }
    
    base.OnStartup(e);
    // ... (reste du code de démarrage)
}
```

**Comportement** :
1. **Première instance** : `isNewInstance = true` → Lance normalement
2. **Instances suivantes** : `isNewInstance = false` → Affiche MessageBox + ferme

**Détails** :
- `new Mutex(true, ...)` : Crée et prend possession du Mutex
- `out isNewInstance` : Indique si c'est la première instance
- `Environment.Exit(0)` : Fermeture immédiate (plus propre que `Shutdown()`)

---

#### 3️⃣ Libération du Mutex (`OnExit`)

```csharp
protected override void OnExit(ExitEventArgs e)
{
    // Libérer le Mutex quand l'application se ferme
    if (instanceMutex != null)
    {
        instanceMutex.ReleaseMutex();
        instanceMutex.Dispose();
    }
    
    base.OnExit(e);
}
```

**Importance** :
- ✅ `ReleaseMutex()` : Libère le verrou système
- ✅ `Dispose()` : Libère les ressources Windows
- ✅ Permet de relancer Panosse après fermeture complète

---

## 📊 SCÉNARIOS D'UTILISATION

### Scénario 1 : Premier lancement ✅

```
Utilisateur double-clic sur Panosse.exe
    ↓
Mutex "Panosse_Unique_Mutex_99" créé
    ↓
isNewInstance = true
    ↓
Application démarre normalement
    ↓
Icône 🧹 apparaît dans le System Tray
```

---

### Scénario 2 : Tentative de second lancement ⚠️

```
Utilisateur double-clic sur Panosse.exe (DÉJÀ active)
    ↓
Tentative de créer Mutex "Panosse_Unique_Mutex_99"
    ↓
Mutex existe déjà !
    ↓
isNewInstance = false
    ↓
MessageBox.Show("Panosse est déjà active...")
    ↓
Utilisateur clique "OK"
    ↓
Environment.Exit(0) → Fermeture immédiate
    ↓
Première instance reste active (inchangée)
```

**Message affiché** :

```
╔═══════════════════════════════════════╗
║   Panosse - Déjà active               ║
╠═══════════════════════════════════════╣
║                                       ║
║  Panosse est déjà active dans la     ║
║  barre des tâches.                    ║
║                                       ║
║  Astuce : Double-cliquez sur l'icône ║
║  🧹 dans la barre des tâches pour     ║
║  afficher la fenêtre.                 ║
║                                       ║
║            [ OK ]                     ║
╚═══════════════════════════════════════╝
```

---

### Scénario 3 : Fermeture complète 🔴

```
Utilisateur : Clic droit System Tray → "Quitter définitivement"
    ↓
Application se ferme (OnExit appelé)
    ↓
instanceMutex.ReleaseMutex()
    ↓
instanceMutex.Dispose()
    ↓
Mutex "Panosse_Unique_Mutex_99" libéré
    ↓
Utilisateur peut relancer Panosse
```

---

## 🧪 TESTS

### Test 1 : Première instance ✅
```powershell
Start-Process Panosse.exe
# Résultat : Application se lance normalement
```

### Test 2 : Seconde instance ✅
```powershell
Start-Process Panosse.exe  # (déjà lancée)
# Résultat : MessageBox "Panosse est déjà active" + Fermeture
```

### Test 3 : Vérification nombre d'instances ✅
```powershell
Get-Process -Name "Panosse"
# Résultat : 1 seule instance (malgré tentatives multiples)
```

### Test 4 : Fermeture + Relancement ✅
```powershell
Get-Process -Name "Panosse" | Stop-Process
Start-Process Panosse.exe
# Résultat : Application se lance à nouveau (Mutex libéré)
```

---

## ✅ AVANTAGES

### Pour l'utilisateur :
- ✅ **Pas de confusion** : Une seule icône dans le System Tray
- ✅ **Message clair** : Sait que Panosse est déjà active
- ✅ **Astuce utile** : Double-clic pour afficher la fenêtre
- ✅ **Pas de ralentissement** : Une seule instance = Moins de RAM

### Pour le système :
- ✅ **Économie RAM** : Une instance au lieu de plusieurs
- ✅ **Pas de conflit** : Un seul hotkey Ctrl+Alt+P actif
- ✅ **Propre** : Mutex libéré correctement à la fermeture

### Pour le développeur :
- ✅ **Code simple** : ~30 lignes de code
- ✅ **Robuste** : Utilise API Windows native (Mutex)
- ✅ **Maintenable** : Facile à comprendre et modifier

---

## 🔒 POURQUOI UN MUTEX ?

### Alternatives considérées

| Méthode | Avantages | Inconvénients | Choix |
|---------|-----------|---------------|-------|
| **Mutex** | Simple, natif Windows, robuste | Nécessite libération | ✅ **CHOISI** |
| Process.GetProcessesByName() | Pas de ressource à libérer | Peut détecter autres apps "Panosse.exe" | ❌ |
| Named Pipe | Communication inter-process | Complexe, overkill | ❌ |
| Fichier .lock | Simple | Peut rester si crash | ❌ |
| Registry Key | Persistent | Pollution registry | ❌ |

**Mutex = Solution optimale** pour single instance Windows.

---

## 📋 CHECKLIST FINALE

- ✅ Mutex créé avec nom unique (`Panosse_Unique_Mutex_99`)
- ✅ Vérification au démarrage (`OnStartup`)
- ✅ MessageBox informatif si instance existante
- ✅ Fermeture immédiate avec `Environment.Exit(0)`
- ✅ Libération propre du Mutex (`OnExit`)
- ✅ Pas de fuite de ressources
- ✅ Code commenté et documenté
- ✅ Tests effectués

---

## 🚀 UTILISATION

### Pour l'utilisateur final :

1. **Lancement normal** :
   - Double-clic sur `Panosse.exe` → Lance l'application

2. **Si Panosse déjà active** :
   - Double-clic sur `Panosse.exe` → Message "Panosse est déjà active"
   - Cliquer "OK" → Message disparaît
   - Panosse reste active dans le System Tray

3. **Pour afficher la fenêtre** :
   - Double-clic sur l'icône 🧹 dans le System Tray

4. **Pour quitter complètement** :
   - Clic droit sur l'icône 🧹 → "Quitter définitivement"

---

## 🎉 CONCLUSION

**Panosse v2.0.0** dispose maintenant d'un système de **single instance robuste** qui :
- ✅ Empêche les instances multiples
- ✅ Informe clairement l'utilisateur
- ✅ Libère proprement les ressources
- ✅ Améliore l'expérience utilisateur

**Le code est propre, testé, et prêt pour la production ! 🧹✨**

---

*Document créé le 3 janvier 2026*
*Version : 2.0.0 - Single Instance avec Mutex*

