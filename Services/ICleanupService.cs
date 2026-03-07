using System.Threading.Tasks;

namespace Panosse.Services;

public interface ICleanupService
{
    Task EmptyRecycleBinAsync();
    long CleanTemporaryFiles();
    long CleanChromeCache();
    long CleanEdgeCache();
    void CleanRegistry();
    long CleanOldDownloads();
    long CleanWindowsLogs();
    long CleanThumbnailCache();
}
