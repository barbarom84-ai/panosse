using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdateAsync(string githubRepo, string currentVersion, CancellationToken cancellationToken = default);
}
