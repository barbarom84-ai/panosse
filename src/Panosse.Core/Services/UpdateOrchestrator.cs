using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

/// <summary>
/// Facade composing download + install for existing callers.
/// </summary>
public sealed class UpdateOrchestrator : IUpdateOrchestrator, IDisposable
{
    private readonly IUpdateDownloadService downloadService;
    private readonly IUpdateInstallService installService;
    private bool disposed;

    public UpdateOrchestrator(
        IUpdateDownloadService downloadService,
        IUpdateInstallService installService)
    {
        this.downloadService = downloadService;
        this.installService = installService;
    }

    public async IAsyncEnumerable<UpdateDownloadProgress> DownloadAndPrepareInstallAsync(
        string downloadUrl,
        string currentExePath,
        string? versionTag,
        string? expectedSha256,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _ = currentExePath; // Reserved for future install-dir aware downloads.
        await foreach (UpdateDownloadProgress progress in downloadService.DownloadAsync(
                           downloadUrl,
                           versionTag,
                           expectedSha256,
                           cancellationToken))
        {
            yield return progress;
        }
    }

    public Task<UpdateInstallResult> BuildInstallScriptAsync(
        string downloadedExePath,
        string currentExePath,
        string? versionTag,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return installService.BuildInstallScriptAsync(downloadedExePath, currentExePath, versionTag, cancellationToken);
    }

    public void LaunchInstallerAndShutdown(string scriptPath)
    {
        ThrowIfDisposed();
        installService.LaunchInstallerAndShutdown(scriptPath);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (downloadService is IDisposable disposableDownload)
        {
            disposableDownload.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(UpdateOrchestrator));
        }
    }
}
