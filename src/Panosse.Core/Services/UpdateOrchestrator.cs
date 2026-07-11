using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class UpdateOrchestrator : IUpdateOrchestrator, IDisposable
{
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
        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

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

        ValidateDownloadedExecutable(targetPath);

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

        string scriptContent = $@"@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
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
start """" ""{currentExePath}""
if exist ""{currentExePath}.old"" del ""{currentExePath}.old""
(goto) 2>nul & del ""%~f0""";

        await File.WriteAllTextAsync(scriptPath, scriptContent, Encoding.UTF8, cancellationToken);
        telemetryService.Increment("update_install_script_generated_count");
        logger.LogInfo("update", $"Install script generated for {versionTag ?? "latest"}.");

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

    private static void ValidateDownloadedExecutable(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists || fileInfo.Length <= 0)
        {
            throw new InvalidOperationException("Le fichier de mise à jour téléchargé est invalide.");
        }

        try
        {
            // Integrity baseline: ensure executable has an Authenticode certificate.
            // Signature chain trust can vary by machine policy; presence check is a safe minimum gate.
#pragma warning disable SYSLIB0057
            _ = X509Certificate.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Le binaire téléchargé n'a pas de signature valide.", ex);
        }
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
