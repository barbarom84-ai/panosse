using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class UpdateInstallService : IUpdateInstallService
{
    /// <summary>
    /// Must match AppId in Panosse-Setup.iss (Inno writes {GUID}_is1 under Uninstall).
    /// </summary>
    private const string InnoUninstallKeyId = "{8E5F4A3B-2D1C-4E9F-A7B6-3C8D9E2F1A4B}_is1";

    private readonly ITelemetryService telemetryService;
    private readonly IOperationHistoryService historyService;
    private readonly ILoggerService logger;

    public UpdateInstallService(
        ITelemetryService telemetryService,
        IOperationHistoryService historyService,
        ILoggerService logger)
    {
        this.telemetryService = telemetryService;
        this.historyService = historyService;
        this.logger = logger;
    }

    public async Task<UpdateInstallResult> BuildInstallScriptAsync(
        string downloadedExePath,
        string currentExePath,
        string? versionTag,
        CancellationToken cancellationToken = default)
    {
        string tempDir = Path.GetTempPath();
        string scriptPath = Path.Combine(tempDir, "PanosseUpdate.bat");
        string registryScriptPath = Path.Combine(tempDir, "PanosseUpdateRegistry.ps1");
        bool requiresElevation = RequiresElevation(currentExePath);
        string displayVersion = ResolveDisplayVersion(versionTag, downloadedExePath);

        string registryScript = BuildRegistrySyncScript(displayVersion, currentExePath);
        await File.WriteAllTextAsync(
            registryScriptPath,
            registryScript,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        string scriptContent = $@"@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
{(requiresElevation ? """
net session >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs -WindowStyle Hidden"
    exit /b 0
)
""" : string.Empty)}
echo Mise a jour de Panosse en cours...
echo.
set /a compteur=0
:attendre
timeout /t 1 /nobreak >nul
tasklist /FI ""IMAGENAME eq Panosse.exe"" 2>NUL | find /I /N ""Panosse.exe"">NUL
if ""%ERRORLEVEL%""==""0"" (
    set /a compteur+=1
    if !compteur! lss 15 goto attendre
)
if exist ""{currentExePath}.old"" del ""{currentExePath}.old""
move /Y ""{currentExePath}"" ""{currentExePath}.old"" >nul 2>&1
move /Y ""{downloadedExePath}"" ""{currentExePath}"" >nul 2>&1
if errorlevel 1 (
    move /Y ""{currentExePath}.old"" ""{currentExePath}"" >nul 2>&1
    exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -File ""{registryScriptPath}"" >nul 2>&1
if not exist ""%APPDATA%\Panosse"" mkdir ""%APPDATA%\Panosse"" >nul 2>&1
echo success {displayVersion}> ""%APPDATA%\Panosse\last-update-result.txt""
start """" ""{currentExePath}""
if exist ""{currentExePath}.old"" del ""{currentExePath}.old""
if exist ""{registryScriptPath}"" del ""{registryScriptPath}""
(goto) 2>nul & del ""%~f0""";

        await File.WriteAllTextAsync(scriptPath, scriptContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
        telemetryService.Increment("update_install_script_generated_count");
        logger.LogInfo(
            "update",
            $"Install script generated for {displayVersion} (elevation={(requiresElevation ? "yes" : "no")}).");

        return new UpdateInstallResult
        {
            Success = true,
            DownloadedExePath = downloadedExePath,
            ScriptPath = scriptPath
        };
    }

    public void LaunchInstallerAndShutdown(string scriptPath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(processInfo);
        telemetryService.Increment("update_install_script_launched_count");
        historyService.AddEntry(new OperationHistoryEntry
        {
            OperationType = "update_install",
            Outcome = "script_launched",
            Details = scriptPath
        });
        Environment.Exit(0);
    }

    private static string ResolveDisplayVersion(string? versionTag, string downloadedExePath)
    {
        string? fromTag = UpdateVersion.Normalize(versionTag);
        if (!string.IsNullOrWhiteSpace(fromTag))
        {
            return fromTag;
        }

        try
        {
            if (File.Exists(downloadedExePath))
            {
                string? fileVersion = FileVersionInfo.GetVersionInfo(downloadedExePath).FileVersion;
                string? normalized = UpdateVersion.Normalize(fileVersion);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    return normalized;
                }
            }
        }
        catch
        {
            // Best effort only.
        }

        return "unknown";
    }

    private static string BuildRegistrySyncScript(string displayVersion, string currentExePath)
    {
        string escapedExe = currentExePath.Replace("'", "''", StringComparison.Ordinal);
        return $$$"""
$ErrorActionPreference = 'SilentlyContinue'
$version = '{{{displayVersion}}}'
$displayName = "Panosse $version"
$exePath = '{{{escapedExe}}}'
$keys = @(
  'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{{{InnoUninstallKeyId}}}',
  'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{{{InnoUninstallKeyId}}}'
)

function Update-PanosseUninstallKey([string]$keyPath) {
  if (-not (Test-Path -LiteralPath $keyPath)) { return }
  Set-ItemProperty -LiteralPath $keyPath -Name 'DisplayVersion' -Value $version -Type String -Force
  Set-ItemProperty -LiteralPath $keyPath -Name 'DisplayName' -Value $displayName -Type String -Force
  if (Test-Path -LiteralPath $exePath) {
    $sizeKb = [int][math]::Round((Get-Item -LiteralPath $exePath).Length / 1KB)
    if ($sizeKb -gt 0) {
      Set-ItemProperty -LiteralPath $keyPath -Name 'EstimatedSize' -Value $sizeKb -Type DWord -Force
    }
  }
}

foreach ($key in $keys) { Update-PanosseUninstallKey $key }

$roots = @(
  'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
  'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
)
foreach ($root in $roots) {
  if (-not (Test-Path -LiteralPath $root)) { continue }
  Get-ChildItem -LiteralPath $root | ForEach-Object {
    $props = Get-ItemProperty -LiteralPath $_.PSPath
    if ($null -ne $props.DisplayName -and $props.DisplayName -like 'Panosse*') {
      Update-PanosseUninstallKey $_.PSPath
    }
  }
}
""";
    }

    private static bool RequiresElevation(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return false;
        }

        string fullPath = Path.GetFullPath(exePath);
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return fullPath.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase);
    }
}
