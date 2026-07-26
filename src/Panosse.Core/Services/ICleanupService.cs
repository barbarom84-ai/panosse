using System.Collections.Generic;
using System.Threading.Tasks;

namespace Panosse.Services;

public interface ICleanupService
{
    Task EmptyRecycleBinAsync();
    long CleanTemporaryFiles();
    long CleanChromeCache();
    long CleanEdgeCache();
    long CleanFirefoxCache();
    long CleanOperaCache();
    long CleanBraveCache();
    long CleanVivaldiCache();
    void CleanRegistry();
    long CleanOldDownloads(IEnumerable<string>? exclusionPatterns = null);
    long CleanWindowsLogs();
    long CleanThumbnailCache();
}
