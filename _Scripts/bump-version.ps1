param(
    [Parameter(Mandatory = $true)]
    [string]$NewVersion
)

if ($NewVersion -notmatch '^\d+\.\d+\.\d+$') {
    Write-Host "ERREUR: format de version invalide (attendu X.Y.Z)." -ForegroundColor Red
    exit 1
}

$csprojPath = "src/Panosse.WinUI/Panosse.WinUI.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Host "ERREUR: $csprojPath introuvable." -ForegroundColor Red
    exit 1
}

$content = Get-Content $csprojPath -Raw
$content = $content -replace '<Version>[\d.]+</Version>', "<Version>$NewVersion</Version>"
$content = $content -replace '<AssemblyVersion>[\d.]+</AssemblyVersion>', "<AssemblyVersion>$NewVersion.0</AssemblyVersion>"
$content = $content -replace '<FileVersion>[\d.]+</FileVersion>', "<FileVersion>$NewVersion.0</FileVersion>"
Set-Content $csprojPath $content -NoNewline

Write-Host "Version mise a jour dans $csprojPath -> $NewVersion" -ForegroundColor Green
