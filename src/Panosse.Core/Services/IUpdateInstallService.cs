using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface IUpdateInstallService
{
    Task<UpdateInstallResult> BuildInstallScriptAsync(
        string downloadedExePath,
        string currentExePath,
        string? versionTag,
        CancellationToken cancellationToken = default);

    void LaunchInstallerAndShutdown(string scriptPath);
}
