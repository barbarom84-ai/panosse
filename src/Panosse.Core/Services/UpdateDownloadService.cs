using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class UpdateDownloadService : IUpdateDownloadService, IDisposable
{
    private readonly HttpClient httpClient;
    private readonly ITelemetryService telemetryService;
    private readonly IOperationHistoryService historyService;
    private bool disposed;

    public UpdateDownloadService(
        ITelemetryService telemetryService,
        IOperationHistoryService historyService)
    {
        this.telemetryService = telemetryService;
        this.historyService = historyService;
        httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Panosse-App");
    }

    public async IAsyncEnumerable<UpdateDownloadProgress> DownloadAsync(
        string downloadUrl,
        string? versionTag,
        string? expectedSha256,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        telemetryService.Increment("update_install_start_count");
        yield return new UpdateDownloadProgress { ProgressPercent = 0, Message = "Téléchargement de la mise à jour..." };

        string targetVersion = string.IsNullOrWhiteSpace(versionTag) ? "latest" : versionTag.Trim();
        string targetPath = Path.Combine(Path.GetTempPath(), $"Panosse-{targetVersion}.exe");

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
                bool shouldEmit = progress != lastProgress &&
                    (progress - lastProgress >= 2 || (DateTime.UtcNow - lastUiUpdateUtc).TotalMilliseconds >= 200);
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

        yield return new UpdateDownloadProgress
        {
            ProgressPercent = 100,
            Message = "Téléchargement terminé",
            LocalFilePath = targetPath
        };
    }

    internal static void ValidateDownloadedExecutable(string filePath, string? expectedSha256)
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
            try { File.Delete(filePath); }
            catch { /* best effort */ }

            throw new InvalidOperationException("L'empreinte SHA256 du binaire téléchargé ne correspond pas à la release.");
        }
    }

    internal static string ComputeSha256Hex(string filePath)
    {
        using var sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(filePath);
        return Convert.ToHexString(sha256.ComputeHash(stream));
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
            throw new ObjectDisposedException(nameof(UpdateDownloadService));
        }
    }
}
