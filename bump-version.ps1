param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion  # Ex: "1.0.1"
)

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   BUMP VERSION PANOSSE" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Valider le format de version
if ($NewVersion -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "ERREUR: Format de version invalide." -ForegroundColor Red
    Write-Host "Utilisez le format X.Y.Z (ex: 1.0.1)" -ForegroundColor Yellow
    exit 1
}

Write-Host "Nouvelle version: $NewVersion" -ForegroundColor Cyan
Write-Host ""

# Chemin du fichier .csproj
$csprojPath = "Panosse.csproj"

if (-not (Test-Path $csprojPath)) {
    Write-Host "ERREUR: Fichier $csprojPath introuvable." -ForegroundColor Red
    exit 1
}

# Lire le contenu
$content = Get-Content $csprojPath -Raw

# Sauvegarder l'ancienne version pour information
if ($content -match '<Version>([\d.]+)</Version>') {
    $oldVersion = $matches[1]
    Write-Host "Version actuelle: $oldVersion" -ForegroundColor Yellow
    Write-Host "Mise a jour vers: $NewVersion" -ForegroundColor Green
    Write-Host ""
}

# Mettre à jour les balises de version
$content = $content -replace '<Version>[\d.]+</Version>', "<Version>$NewVersion</Version>"
$content = $content -replace '<AssemblyVersion>[\d.]+</AssemblyVersion>', "<AssemblyVersion>$NewVersion.0</AssemblyVersion>"
$content = $content -replace '<FileVersion>[\d.]+</FileVersion>', "<FileVersion>$NewVersion.0</FileVersion>"

# Écrire le fichier modifié
Set-Content $csprojPath $content -NoNewline

Write-Host "[OK] Version mise a jour dans $csprojPath" -ForegroundColor Green
Write-Host ""

# Afficher les modifications
Write-Host "Modifications appliquees:" -ForegroundColor Cyan
Write-Host "  <Version>$NewVersion</Version>" -ForegroundColor White
Write-Host "  <AssemblyVersion>$NewVersion.0</AssemblyVersion>" -ForegroundColor White
Write-Host "  <FileVersion>$NewVersion.0</FileVersion>" -ForegroundColor White
Write-Host ""

# Demander si on doit commit
Write-Host "Voulez-vous commiter ces changements ? (o/N): " -NoNewline -ForegroundColor Yellow
$commit = Read-Host

if ($commit -eq "o" -or $commit -eq "O") {
    Write-Host ""
    Write-Host "Commit en cours..." -ForegroundColor Cyan
    
    $gitPath = "C:\Program Files\Git\bin\git.exe"
    if (Test-Path $gitPath) {
        & $gitPath add $csprojPath
        & $gitPath commit -m "Bump version to $NewVersion"
        
        Write-Host "[OK] Commit cree" -ForegroundColor Green
        Write-Host ""
        
        # Demander si on doit push
        Write-Host "Voulez-vous pusher vers GitHub ? (o/N): " -NoNewline -ForegroundColor Yellow
        $push = Read-Host
        
        if ($push -eq "o" -or $push -eq "O") {
            Write-Host ""
            Write-Host "Push en cours..." -ForegroundColor Cyan
            & $gitPath push
            Write-Host "[OK] Push effectue" -ForegroundColor Green
            Write-Host ""
        }
        
        # Proposer de créer la release
        Write-Host "Voulez-vous creer la release maintenant ? (o/N): " -NoNewline -ForegroundColor Yellow
        $release = Read-Host
        
        if ($release -eq "o" -or $release -eq "O") {
            Write-Host ""
            if (Test-Path "release-simple.ps1") {
                .\release-simple.ps1 -Version $NewVersion
            } else {
                Write-Host "Script release-simple.ps1 introuvable." -ForegroundColor Yellow
                Write-Host "Creez la release manuellement avec:" -ForegroundColor Cyan
                Write-Host "  .\release-simple.ps1 -Version `"$NewVersion`"" -ForegroundColor White
            }
        }
    } else {
        Write-Host "Git non trouve. Commitez manuellement avec:" -ForegroundColor Yellow
        Write-Host "  git add $csprojPath" -ForegroundColor White
        Write-Host "  git commit -m `"Bump version to $NewVersion`"" -ForegroundColor White
        Write-Host "  git push" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "   VERSION MISE A JOUR AVEC SUCCES !" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""

