# Script pour tester Panosse avec logs

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   TEST PANOSSE AVEC LOGS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Nettoyer les anciens logs
$desktopPath = [Environment]::GetFolderPath("Desktop")
$debugLog = Join-Path $desktopPath "panosse_debug.log"
$crashLog = Join-Path $desktopPath "panosse_crash.log"

if (Test-Path $debugLog) {
    Remove-Item $debugLog -Force
    Write-Host "Ancien panosse_debug.log supprime" -ForegroundColor Gray
}

if (Test-Path $crashLog) {
    Remove-Item $crashLog -Force
    Write-Host "Ancien panosse_crash.log supprime" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Lancement de Panosse..." -ForegroundColor Yellow
Write-Host ""

# Lancer Panosse
$exePath = "bin\Release\net8.0-windows\win-x64\Panosse.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "ERREUR : $exePath introuvable" -ForegroundColor Red
    Write-Host "Compilez d'abord avec : dotnet build -c Release" -ForegroundColor Yellow
    exit 1
}

# Lancer en background
$process = Start-Process $exePath -PassThru

Write-Host "Panosse lance (PID: $($process.Id))" -ForegroundColor Green
Write-Host ""
Write-Host "Attente de 5 secondes..." -ForegroundColor Gray
Start-Sleep -Seconds 5

# Vérifier si le processus tourne toujours
$running = Get-Process -Id $process.Id -ErrorAction SilentlyContinue

if ($running) {
    Write-Host ""
    Write-Host "SUCCES : Panosse fonctionne !" -ForegroundColor Green
    Write-Host ""
    Write-Host "Processus actif : PID $($running.Id)" -ForegroundColor Gray
    
    # Afficher le log de debug
    if (Test-Path $debugLog) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "   CONTENU DE panosse_debug.log" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        Get-Content $debugLog
    } else {
        Write-Host ""
        Write-Host "Aucun log de debug genere" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Fermeture de Panosse..." -ForegroundColor Yellow
    Stop-Process -Id $running.Id -Force -ErrorAction SilentlyContinue
    Write-Host "Ferme" -ForegroundColor Gray
    
} else {
    Write-Host ""
    Write-Host "ERREUR : Panosse a crash !" -ForegroundColor Red
    Write-Host ""
    
    # Afficher les logs
    if (Test-Path $debugLog) {
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "   CONTENU DE panosse_debug.log" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Get-Content $debugLog
        Write-Host ""
    }
    
    if (Test-Path $crashLog) {
        Write-Host "========================================" -ForegroundColor Red
        Write-Host "   CONTENU DE panosse_crash.log" -ForegroundColor Red
        Write-Host "========================================" -ForegroundColor Red
        Write-Host ""
        Get-Content $crashLog
        Write-Host ""
    } else {
        Write-Host "Aucun crash log genere (crash tres precoce ?)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Les logs sont disponibles sur votre Bureau :" -ForegroundColor White
Write-Host "  - panosse_debug.log" -ForegroundColor Gray
Write-Host "  - panosse_crash.log (si crash)" -ForegroundColor Gray
Write-Host ""

