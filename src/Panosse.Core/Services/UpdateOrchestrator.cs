using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class UpdateOrchestrator : IUpdateOrchestrator, IDisposable
{
    /// <summary>
    /// Must match AppId in Panosse-Setup.iss (Inno writes {GUID}_is1 under Uninstall).
    /// </summary>
    private const string InnoUninstallKeyId = "{8E5F4A3B-2D1C-4E9F-A7B6-3C8D9E2F1A4B}_is1";

    private readonly HttpClient httpClient;
    private readonly ITelemetryService telemetryService;
    private readonly IOperationHistoryService historyService;
    private readonly ILoggerService logger;
    private bool disposed;

    public UpdateOrchestrator(
        ITelemetryService telemetryService,
        IOperationHistoryService historyService,
        ILoggerService logger)
    {
        this.telemetryService = telemetryService;
        this.historyService = historyService;
        this.logger = logger;

        httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Panosse-App");
    }

    public async IAsyncEnumerable<UpdateDownloadProgress> DownloadAndPrepareInstallAsync(
        string downloadUrl,
        string currentExePath,
        string? versionTag,
        string? expectedSha256,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();

        telemetryService.Increment("update_install_start_count");
        yield return new UpdateDownloadProgress { ProgressPercent = 0, Message = "Téléchargement de la mise à jour..." };

        string tempDir = Path.GetTempPath();
        string targetVersion = string.IsNullOrWhiteSpace(versionTag) ? "latest" : versionTag.Trim();
        string targetPath = Path.Combine(tempDir, $"Panosse-{targetVersion}.exe");

        using HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        long totalBytes = response.Content.Headers.ContentLength ?? 0;

        await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            long totalRead = 0;
            int read;
            int lastProgress = -1;
            DateTime lastUiUpdateUtc = DateTime.UtcNow;

            while ((read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;

                if (totalBytes <= 0)
                {
                    continue;
                }

                int progress = (int)((totalRead * 100) / totalBytes);
                bool shouldEmit = progress != lastProgress && (progress - lastProgress >= 2 || (DateTime.UtcNow - lastUiUpdateUtc).TotalMilliseconds >= 200);
                if (!shouldEmit)
                {
                    continue;
                }

                lastProgress = progress;
                lastUiUpdateUtc = DateTime.UtcNow;
                yield return new UpdateDownloadProgress
                {
                    ProgressPercent = progress,
                    Message = $"Téléchargement de la mise à jour... {progress}%"
                };
            }

            await fileStream.FlushAsync(cancellationToken);
        }

        ValidateDownloadedExecutable(targetPath, expectedSha256);

        telemetryService.Increment("update_install_download_success_count");
        sw.Stop();
        telemetryService.RecordDuration("update_download_duration_ms", sw.Elapsed);
        historyService.AddEntry(new OperationHistoryEntry
        {
            OperationType = "update_download",
            Outcome = "success",
            DurationMs = (long)sw.Elapsed.TotalMilliseconds,
            Details = targetPath
        });

        yield return new UpdateDownloadProgress { ProgressPercent = 100, Message = "Téléchargement terminé" };
    }

    public async Task<UpdateInstallResult> BuildInstallScriptAsync(
        string downloadedExePath,
        string currentExePath,
        string? versionTag,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
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
        ThrowIfDisposed();
        var processInfo = new ProcessStartInfo
        {
            FileName = scriptPath,
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(processInfo);
        telemetryService.Increment("update_install_success_count");
        Environment.Exit(0);
    }

    private static void ValidateDownloadedExecutable(string filePath, string? expectedSha256)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists || fileInfo.Length <= 0)
        {
            throw new InvalidOperationException("Le fichier de mise à jour téléchargé est invalide.");
        }

        if (string.IsNullOrWhiteSpace(expectedSha256))
        {
            throw new InvalidOperationException("Empreinte SHA256 indisponible pour cette mise à jour.");
        }

        string actualSha256 = ComputeSha256Hex(filePath);
        if (!string.Equals(actualSha256, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Best effort cleanup of corrupted download.
            }

            throw new InvalidOperationException("L'empreinte SHA256 du binaire téléchargé ne correspond pas à la release.");
        }
    }

    private static string ComputeSha256Hex(string filePath)
    {
        using var sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(filePath);
        byte[] hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    private static string ResolveDisplayVersion(string? versionTag, string downloadedExePath)
    {
        string? fromTag = NormalizeVersion(versionTag);
        if (!string.IsNullOrWhiteSpace(fromTag))
        {
            return fromTag;
        }

        try
        {
            if (File.Exists(downloadedExePath))
            {
                string? fileVersion = FileVersionInfo.GetVersionInfo(downloadedExePath).FileVersion;
                string? normalized = NormalizeVersion(fileVersion);
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

    private static string? NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version) ||
            version.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string trimmed = version.Trim().TrimStart('v', 'V');
        string[] parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 3)
        {
            return $"{parts[0]}.{parts[1]}.{parts[2]}";
        }

        return trimmed.Length > 0 ? trimmed : null;
    }

    private static string BuildRegistrySyncScript(string displayVersion, string currentExePath)
    {
        // Keep the script ASCII-simple: update Inno uninstall metadata so
        // "Programmes et fonctionnalités" matches the replaced binary.
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

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        httpClient.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(UpdateOrchestrator));
        }
    }
}
