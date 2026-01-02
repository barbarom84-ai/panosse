# Script de creation automatique de l'installateur Panosse
# Ce script compile l'application et cree l'installateur Inno Setup

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Creation de l'installateur Panosse  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Etape 1 : Compiler l'application
Write-Host "Etape 1/3 : Compilation de l'application..." -ForegroundColor Yellow
Write-Host ""

if (Test-Path ".\publish") {
    Write-Host "  Nettoyage de l'ancienne publication..." -ForegroundColor Gray
    Remove-Item ".\publish" -Recurse -Force
}

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish

if ($LASTEXITCODE -ne 0) {
    Write-Host "  [ERREUR] Erreur lors de la compilation" -ForegroundColor Red
    exit 1
}

Write-Host "  [OK] Application compilee avec succes" -ForegroundColor Green
Write-Host ""

# Etape 2 : Verifier Inno Setup
Write-Host "Etape 2/3 : Verification d'Inno Setup..." -ForegroundColor Yellow

$innoSetupPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($path in $innoSetupPaths) {
    if (Test-Path $path) {
        $isccPath = $path
        break
    }
}

if ($null -eq $isccPath) {
    Write-Host "  [ERREUR] Inno Setup n'est pas installe !" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Veuillez telecharger et installer Inno Setup :" -ForegroundColor Yellow
    Write-Host "  https://jrsoftware.org/isinfo.php" -ForegroundColor Cyan
    Write-Host ""
    exit 1
}

Write-Host "  [OK] Inno Setup trouve : $isccPath" -ForegroundColor Green
Write-Host ""

# Etape 3 : Creer l'installateur
Write-Host "Etape 3/3 : Creation de l'installateur..." -ForegroundColor Yellow
Write-Host ""

# Creer le dossier installer s'il n'existe pas
if (-not (Test-Path ".\installer")) {
    New-Item -ItemType Directory -Path ".\installer" | Out-Null
}

# Compiler le script Inno Setup
& $isccPath "Panosse-Setup.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host "  [ERREUR] Erreur lors de la creation de l'installateur" -ForegroundColor Red
    exit 1
}

Write-Host "  [OK] Installateur cree avec succes" -ForegroundColor Green
Write-Host ""

# Afficher les informations sur l'installateur
Write-Host "========================================" -ForegroundColor Green
Write-Host "   Creation terminee avec succes !    " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$installerFiles = Get-ChildItem ".\installer\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($installerFiles) {
    $fileInfo = Get-Item $installerFiles.FullName
    $fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 2)
    
    Write-Host "Fichier installateur :" -ForegroundColor Cyan
    Write-Host "   Nom         : $($fileInfo.Name)" -ForegroundColor White
    Write-Host "   Taille      : $fileSizeMB Mo" -ForegroundColor White
    Write-Host "   Emplacement : $($fileInfo.FullName)" -ForegroundColor White
    Write-Host ""
    
    # Calculer le hash SHA256
    Write-Host "Hash SHA256 (pour verification d'integrite) :" -ForegroundColor Cyan
    $hash = (Get-FileHash $fileInfo.FullName -Algorithm SHA256).Hash
    Write-Host "   $hash" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "L'installateur est pret a etre distribue !" -ForegroundColor Green
    Write-Host ""
    Write-Host "Pour tester : " -ForegroundColor Yellow -NoNewline
    Write-Host "Start-Process '$($fileInfo.FullName)'" -ForegroundColor White
} else {
    Write-Host "Attention : Fichier installateur introuvable" -ForegroundColor Yellow
}
