param(
    [Parameter(Mandatory=$false)]
    [string]$Version
)

# Couleurs
$ColorSuccess = "Green"
$ColorError = "Red"
$ColorWarning = "Yellow"
$ColorInfo = "Cyan"

Write-Host ""
Write-Host "============================================" -ForegroundColor Magenta
Write-Host "   PANOSSE - RELEASE AUTOMATIQUE" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""

# Vérifier que Git est installé
$gitPath = "C:\Program Files\Git\bin\git.exe"
if (-not (Test-Path $gitPath)) {
    Write-Host "ERREUR: Git n'est pas installe." -ForegroundColor $ColorError
    exit 1
}

# Demander la version si non fournie
if (-not $Version) {
    Write-Host "Entrez le numero de version (ex: 1.0.0): " -NoNewline -ForegroundColor $ColorInfo
    $Version = Read-Host
}

# Valider le format
if ($Version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9]+)?$') {
    Write-Host "ERREUR: Format invalide. Utilisez X.Y.Z (ex: 1.0.0)" -ForegroundColor $ColorError
    exit 1
}

$TagName = "v$Version"

Write-Host ""
Write-Host "[INFO] Version: $Version" -ForegroundColor $ColorInfo
Write-Host "[INFO] Tag: $TagName" -ForegroundColor $ColorInfo
Write-Host ""

# Vérifier la branche
$currentBranch = & $gitPath rev-parse --abbrev-ref HEAD 2>&1
if ($currentBranch -ne "main") {
    Write-Host "ATTENTION: Vous n'etes pas sur la branche 'main' (branche: $currentBranch)" -ForegroundColor $ColorWarning
    Write-Host "Continuer ? (o/N): " -NoNewline -ForegroundColor $ColorWarning
    $continue = Read-Host
    if ($continue -ne "o" -and $continue -ne "O") {
        Write-Host "Annule." -ForegroundColor $ColorError
        exit 1
    }
} else {
    Write-Host "[OK] Branche: main" -ForegroundColor $ColorSuccess
}

# Vérifier les modifications non commitées
$status = & $gitPath status --porcelain 2>&1
if ($status) {
    Write-Host "ERREUR: Modifications non commitees detectees." -ForegroundColor $ColorError
    Write-Host "Commitez d'abord avec:" -ForegroundColor $ColorWarning
    Write-Host "  git add ." -ForegroundColor White
    Write-Host "  git commit -m 'Votre message'" -ForegroundColor White
    Write-Host "  git push" -ForegroundColor White
    exit 1
}
Write-Host "[OK] Aucune modification non commitee" -ForegroundColor $ColorSuccess

# Pull
Write-Host ""
Write-Host "[INFO] Pull des derniers changements..." -ForegroundColor $ColorInfo
$pullResult = & $gitPath pull 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERREUR lors du pull: $pullResult" -ForegroundColor $ColorError
    exit 1
}
Write-Host "[OK] Pull effectue" -ForegroundColor $ColorSuccess

# Vérifier si le tag existe
$existingTag = & $gitPath tag -l $TagName 2>&1
if ($existingTag) {
    Write-Host "ERREUR: Le tag '$TagName' existe deja." -ForegroundColor $ColorError
    Write-Host "Supprimez-le avec:" -ForegroundColor $ColorWarning
    Write-Host "  git tag -d $TagName" -ForegroundColor White
    Write-Host "  git push origin --delete $TagName" -ForegroundColor White
    exit 1
}

# Créer le tag
Write-Host ""
Write-Host "[INFO] Creation du tag $TagName..." -ForegroundColor $ColorInfo
$tagResult = & $gitPath tag $TagName -m "Release $Version" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERREUR lors de la creation du tag: $tagResult" -ForegroundColor $ColorError
    exit 1
}
Write-Host "[OK] Tag $TagName cree" -ForegroundColor $ColorSuccess

# Pousser le tag
Write-Host "[INFO] Push du tag vers GitHub..." -ForegroundColor $ColorInfo
$pushResult = & $gitPath push origin $TagName 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERREUR lors du push: $pushResult" -ForegroundColor $ColorError
    exit 1
}
Write-Host "[OK] Tag pousse vers GitHub" -ForegroundColor $ColorSuccess

# Récupérer le dépôt
$remoteUrl = & $gitPath config --get remote.origin.url 2>&1
$repoName = ""
if ($remoteUrl -match 'github\.com[:/](.+?)(?:\.git)?$') {
    $repoName = $matches[1]
}

# Résultat final
Write-Host ""
Write-Host "============================================" -ForegroundColor $ColorSuccess
Write-Host "   RELEASE EN COURS DE CREATION !" -ForegroundColor $ColorSuccess
Write-Host "============================================" -ForegroundColor $ColorSuccess
Write-Host ""

if ($repoName) {
    Write-Host "Suivre le workflow:" -ForegroundColor $ColorInfo
    Write-Host "  https://github.com/$repoName/actions" -ForegroundColor White
    Write-Host ""
    Write-Host "Release disponible dans ~5 minutes:" -ForegroundColor $ColorInfo
    Write-Host "  https://github.com/$repoName/releases/tag/$TagName" -ForegroundColor White
    Write-Host ""
}

Write-Host "Duree estimee: 5 minutes" -ForegroundColor $ColorWarning
Write-Host "Fichier genere: Panosse-$TagName.exe" -ForegroundColor $ColorInfo
Write-Host ""

Write-Host "Le workflow GitHub Actions va:" -ForegroundColor $ColorInfo
Write-Host "  1. Compiler le projet en Release" -ForegroundColor White
Write-Host "  2. Creer un Single File .exe" -ForegroundColor White
Write-Host "  3. Calculer le checksum SHA256" -ForegroundColor White
Write-Host "  4. Creer la GitHub Release" -ForegroundColor White
Write-Host "  5. Uploader l'executable" -ForegroundColor White
Write-Host ""
Write-Host "Tout est automatique ! Aucune action requise." -ForegroundColor $ColorSuccess
Write-Host ""

