using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface IUpdateOrchestrator
{
    IAsyncEnumerable<UpdateDownloadProgress> DownloadAndPrepareInstallAsync(
        string downloadUrl,
        string currentExePath,
        string? versionTag,
        string? expectedSha256,
        CancellationToken cancellationToken = default);

    Task<UpdateInstallResult> BuildInstallScriptAsync(
        string downloadedExePath,
        string currentExePath,
        string? versionTag,
        CancellationToken cancellationToken = default);

    void LaunchInstallerAndShutdown(string scriptPath);
}
