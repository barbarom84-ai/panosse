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
    long CleanOldDownloads(IEnumerable<string>? exclusionPatterns = null);
    long CleanWindowsLogs();
    long CleanThumbnailCache();
    long CleanTemporaryInternetFiles();
    long CleanDeliveryOptimization();
    long CleanWindowsErrorReports();
    long CleanDefenderArtifacts();
    long CleanObsoleteDriverPackages();
    long CleanShaderCaches();
    long CleanCrashDumps();
    long CleanWindowsUpdateDownloads();
    long CleanPreviousWindowsInstallations();
    long CleanDeveloperCaches();
}
