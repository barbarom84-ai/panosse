using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface IBrowserProcessService
{
    Task<IReadOnlyList<string>> GetRunningBrowsersAsync(CancellationToken cancellationToken = default);

    Task<BrowserCloseResult> CloseBrowsersAsync(
        IEnumerable<string> browserNames,
        CancellationToken cancellationToken = default);
}

public sealed class BrowserCloseResult
{
    public IReadOnlyList<string> ClosedBrowsers { get; init; } = [];
    public IReadOnlyList<string> RemainingBrowsers { get; init; } = [];
    public bool AllClosed => RemainingBrowsers.Count == 0;
}
