using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface ICleanupOrchestrator
{
    IAsyncEnumerable<CleanupStepUpdate> StreamCleanupAsync(CleanupExecutionOptions options, CancellationToken cancellationToken = default);
    Task<CleanupRunResult> RunBackgroundCleanupAsync(CleanupExecutionOptions options, CancellationToken cancellationToken = default);
}
