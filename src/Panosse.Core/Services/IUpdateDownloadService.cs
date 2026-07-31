using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface IUpdateDownloadService
{
    IAsyncEnumerable<UpdateDownloadProgress> DownloadAsync(
        string downloadUrl,
        string? versionTag,
        string? expectedSha256,
        CancellationToken cancellationToken = default);
}
