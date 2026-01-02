<#
.SYNOPSIS
    Script de release automatique pour Panosse
    
.DESCRIPTION
    Crée un tag Git et déclenche automatiquement le workflow GitHub Actions
    pour compiler et publier une nouvelle release.
    
.PARAMETER Version
    Numéro de version (ex: 1.0.0, 1.2.3, 2.0.0-beta)
    
.EXAMPLE
    .\release.ps1
    (Mode interactif)
    
.EXAMPLE
    .\release.ps1 -Version "1.0.0"
    (Mode automatique)
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$Version
)

# Couleurs
$ColorSuccess = "Green"
$ColorError = "Red"
$ColorWarning = "Yellow"
$ColorInfo = "Cyan"
$ColorTitle = "Magenta"

# Fonction pour afficher un titre
function Show-Title {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════╗" -ForegroundColor $ColorTitle
    Write-Host "║   🚀 PANOSSE - RELEASE AUTOMATIQUE 🚀   ║" -ForegroundColor $ColorTitle
    Write-Host "╚═══════════════════════════════════════════╝" -ForegroundColor $ColorTitle
    Write-Host ""
}

# Fonction pour afficher un message de succès
function Show-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor $ColorSuccess
}

# Fonction pour afficher une erreur et quitter
function Show-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor $ColorError
    Write-Host ""
    exit 1
}

# Fonction pour afficher un avertissement
function Show-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor $ColorWarning
}

# Fonction pour afficher une info
function Show-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor $ColorInfo
}

# Afficher le titre
Show-Title

# Vérifier que Git est installé
try {
    $gitPath = "C:\Program Files\Git\bin\git.exe"
    if (-not (Test-Path $gitPath)) {
        Show-Error "Git n'est pas installé. Installez-le d'abord."
    }
} catch {
    Show-Error "Impossible de trouver Git."
}

# Demander la version si non fournie
if (-not $Version) {
    Write-Host "Entrez le numéro de version (ex: 1.0.0): " -NoNewline -ForegroundColor $ColorInfo
    $Version = Read-Host
}

# Valider le format de la version
if ($Version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?$') {
    Show-Error "Format de version invalide. Utilisez: X.Y.Z (ex: 1.0.0, 1.2.3-beta)"
}

$TagName = "v$Version"

Write-Host ""
Show-Info "Version: $Version"
Show-Info "Tag: $TagName"
Write-Host ""

# Vérifier la branche actuelle
$currentBranch = & $gitPath rev-parse --abbrev-ref HEAD 2>&1
if ($currentBranch -ne "main") {
    Show-Warning "Vous n'êtes pas sur la branche 'main' (branche actuelle: $currentBranch)"
    Write-Host "Voulez-vous continuer ? (o/N): " -NoNewline -ForegroundColor $ColorWarning
    $continue = Read-Host
    if ($continue -ne "o" -and $continue -ne "O") {
        Show-Error "Annulé par l'utilisateur."
    }
} else {
    Show-Success "Branche: main"
}

# Vérifier qu'il n'y a pas de modifications non commitées
$status = & $gitPath status --porcelain 2>&1
if ($status) {
    Show-Error "Il y a des modifications non commitées. Commitez-les d'abord avec:`n  git add .`n  git commit -m 'Votre message'`n  git push"
}
Show-Success "Aucune modification non commitée"

# Pull les derniers changements
Write-Host ""
Show-Info "Pull des derniers changements..."
$pullResult = & $gitPath pull 2>&1
if ($LASTEXITCODE -ne 0) {
    Show-Error "Erreur lors du pull: $pullResult"
}
Show-Success "Pull effectué"

# Vérifier si le tag existe déjà
$existingTag = & $gitPath tag -l $TagName 2>&1
if ($existingTag) {
    Show-Error "Le tag '$TagName' existe déjà. Utilisez une version différente ou supprimez le tag existant avec:`n  git tag -d $TagName`n  git push origin --delete $TagName"
}

# Créer le tag
Write-Host ""
Show-Info "Création du tag $TagName..."
$tagResult = & $gitPath tag $TagName -m "Release $Version" 2>&1
if ($LASTEXITCODE -ne 0) {
    Show-Error "Erreur lors de la création du tag: $tagResult"
}
Show-Success "Tag $TagName créé"

# Pousser le tag vers GitHub
Show-Info "Push du tag vers GitHub..."
$pushResult = & $gitPath push origin $TagName 2>&1
if ($LASTEXITCODE -ne 0) {
    Show-Error "Erreur lors du push du tag: $pushResult"
}
Show-Success "Tag poussé vers GitHub"

# Récupérer le nom du dépôt
$remoteUrl = & $gitPath config --get remote.origin.url 2>&1
$repoName = ""
if ($remoteUrl -match 'github\.com[:/](.+?)(?:\.git)?$') {
    $repoName = $matches[1]
}

# Afficher les informations finales
Write-Host ""
Write-Host "╔════════════════════════════════════════════╗" -ForegroundColor $ColorSuccess
Write-Host "║  🎉 RELEASE EN COURS DE CRÉATION ! 🎉    ║" -ForegroundColor $ColorSuccess
Write-Host "╚════════════════════════════════════════════╝" -ForegroundColor $ColorSuccess
Write-Host ""

if ($repoName) {
    Write-Host "📊 Suivre la progression du workflow :" -ForegroundColor $ColorInfo
    Write-Host "   https://github.com/$repoName/actions" -ForegroundColor White
    Write-Host ""
    Write-Host "📦 Release disponible dans ~5 minutes :" -ForegroundColor $ColorInfo
    Write-Host "   https://github.com/$repoName/releases/tag/$TagName" -ForegroundColor White
    Write-Host ""
}

Write-Host "⏱️  Durée estimée : 5 minutes" -ForegroundColor $ColorWarning
Write-Host "📥 Fichier généré : Panosse-$TagName.exe" -ForegroundColor $ColorInfo
Write-Host ""

Write-Host "✨ Le workflow GitHub Actions va :" -ForegroundColor $ColorInfo
Write-Host "   1. Compiler le projet en mode Release" -ForegroundColor White
Write-Host "   2. Créer un Single File .exe" -ForegroundColor White
Write-Host "   3. Calculer le checksum SHA256" -ForegroundColor White
Write-Host "   4. Créer la GitHub Release" -ForegroundColor White
Write-Host "   5. Uploader l'exécutable" -ForegroundColor White
Write-Host ""

Write-Host "🎊 Tout est automatique ! Aucune action requise. 🎊" -ForegroundColor $ColorSuccess
Write-Host ""

