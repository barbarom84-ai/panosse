using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class CleanupOrchestrator : ICleanupOrchestrator
{
    private readonly ICleanupService cleanupService;
    private readonly IRegistryCleanerService registryCleaner;
    private readonly ITelemetryService telemetryService;
    private readonly IOperationHistoryService historyService;
    private readonly ILoggerService logger;

    public CleanupOrchestrator(
        ICleanupService cleanupService,
        IRegistryCleanerService registryCleaner,
        ITelemetryService telemetryService,
        IOperationHistoryService historyService,
        ILoggerService logger)
    {
        this.cleanupService = cleanupService;
        this.registryCleaner = registryCleaner;
        this.telemetryService = telemetryService;
        this.historyService = historyService;
        this.logger = logger;
    }

    public async IAsyncEnumerable<CleanupStepUpdate> StreamCleanupAsync(
        CleanupExecutionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        options ??= new CleanupExecutionOptions();
        bool preview = options.PreviewOnly;
        string profile = CleanupProfiles.Normalize(options.CleanupProfile);
        int step = 0;

        var stopwatch = Stopwatch.StartNew();
        long totalFreedBytes = 0;
        string outcome = "success";
        string details = $"profile={profile}";

        telemetryService.Increment(preview ? "cleanup_preview_start_count" : "cleanup_manual_start_count");

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string tempPath = Path.GetTempPath();
        string windowsTemp = @"C:\Windows\Temp";
        string chromeCache = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache");
        string edgeCache = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache");
        string firefoxProfiles = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Mozilla\Firefox\Profiles");
        string thumbnails = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
        string inetCache = Path.Combine(localAppData, @"Microsoft\Windows\INetCache");
        string deliveryCache = @"C:\Windows\SoftwareDistribution\DeliveryOptimization\Cache";
        string werQueue = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Microsoft\Windows\WER\ReportQueue");
        string defenderHistory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Microsoft\Windows Defender\Scans\History");
        string driverStoreTemp = @"C:\Windows\System32\DriverStore\Temp";
        int registryIssueCount = 0;
        RegistryCleanResult? registryResult = null;

        var steps = new List<(string Category, string Start, Func<CancellationToken, Task<long>> Action, Func<long, string> End)>
        {
            (CleanupProfiles.CategoryRecycle, "🗑️ Vidage de la corbeille...", ct => RunOrEstimateAsync(preview, () => { cleanupService.EmptyRecycleBinAsync().GetAwaiter().GetResult(); return 0L; }, 0), _ => "✅ Corbeille traitée"),
            (CleanupProfiles.CategoryTemp, "🧹 Nettoyage des fichiers temporaires...", _ => RunOrEstimateAsync(preview, cleanupService.CleanTemporaryFiles, EstimateDirectoryBytes(tempPath) + EstimateDirectoryBytes(windowsTemp)), b => $"✅ Fichiers temporaires traités ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryInetCache, "🌐 Nettoyage des fichiers Internet temporaires...", _ => RunOrEstimateAsync(preview, cleanupService.CleanTemporaryInternetFiles, EstimateDirectoryBytes(inetCache)), b => $"✅ Internet temporaire traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryBrowser, "🌐 Nettoyage du cache Chrome...", _ => RunOrEstimateAsync(preview, cleanupService.CleanChromeCache, EstimateDirectoryBytes(chromeCache)), b => $"✅ Cache Chrome traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryBrowser, "🌐 Nettoyage du cache Edge...", _ => RunOrEstimateAsync(preview, cleanupService.CleanEdgeCache, EstimateDirectoryBytes(edgeCache)), b => $"✅ Cache Edge traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryBrowser, "🌐 Nettoyage du cache Firefox...", _ => RunOrEstimateAsync(preview, cleanupService.CleanFirefoxCache, EstimateFirefoxCacheBytes(firefoxProfiles)), b => $"✅ Cache Firefox traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryBrowser, "🌐 Nettoyage du cache Opera...", _ => RunOrEstimateAsync(preview, cleanupService.CleanOperaCache, 0), b => $"✅ Cache Opera traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryBrowser, "🌐 Nettoyage du cache Brave...", _ => RunOrEstimateAsync(preview, cleanupService.CleanBraveCache, 0), b => $"✅ Cache Brave traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryBrowser, "🌐 Nettoyage du cache Vivaldi...", _ => RunOrEstimateAsync(preview, cleanupService.CleanVivaldiCache, 0), b => $"✅ Cache Vivaldi traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryDeliveryOptimization, "📦 Nettoyage Delivery Optimization...", _ => RunOrEstimateAsync(preview, cleanupService.CleanDeliveryOptimization, EstimateDirectoryBytes(deliveryCache)), b => $"✅ Delivery Optimization traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryErrorReports, "🩹 Nettoyage des rapports d'erreurs Windows...", _ => RunOrEstimateAsync(preview, cleanupService.CleanWindowsErrorReports, EstimateDirectoryBytes(werQueue)), b => $"✅ Rapports d'erreurs traités ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryDefender, "🛡️ Nettoyage Microsoft Defender...", _ => RunOrEstimateAsync(preview, cleanupService.CleanDefenderArtifacts, EstimateDirectoryBytes(defenderHistory)), b => $"✅ Microsoft Defender traité ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryDrivers, "💾 Nettoyage des composants Windows remplacés (DISM)...", _ => RunOrEstimateAsync(preview, cleanupService.CleanObsoleteDriverPackages, EstimateDirectoryBytes(driverStoreTemp)), b => $"✅ Composants Windows traités ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryRegistry, "📋 Analyse du registre...", ct => Task.Run(() =>
            {
                IReadOnlyList<RegistryIssue> issues = registryCleaner.Scan(profile, options.ExclusionPatterns, ct);
                registryIssueCount = issues.Count;
                if (!preview)
                {
                    registryResult = registryCleaner.Clean(issues, ct);
                }

                return 0L;
            }, ct), _ => FormatRegistryStepMessage(preview, registryIssueCount, registryResult)),
            (CleanupProfiles.CategoryDownloads, "📥 Nettoyage des téléchargements anciens...", _ => RunOrEstimateAsync(preview, () => cleanupService.CleanOldDownloads(options.ExclusionPatterns), EstimateOldDownloads(options.ExclusionPatterns)), b => $"✅ Téléchargements traités ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryLogs, "📄 Nettoyage des logs Windows...", _ => RunOrEstimateAsync(preview, cleanupService.CleanWindowsLogs, EstimateDirectoryBytes(@"C:\Windows\Logs") + EstimateDirectoryBytes(@"C:\Windows\System32\LogFiles")), b => $"✅ Logs Windows traités ({ToMb(b)} Mo)"),
            (CleanupProfiles.CategoryThumbnails, "🖼️ Nettoyage du cache des miniatures...", _ => RunOrEstimateAsync(preview, cleanupService.CleanThumbnailCache, EstimateThumbnailBytes(thumbnails)), b => $"✅ Cache miniatures traité ({ToMb(b)} Mo)")
        };

        List<(string Category, string Start, Func<CancellationToken, Task<long>> Action, Func<long, string> End)> filteredSteps =
            steps.Where(s => CleanupProfiles.IncludesCategory(profile, s.Category)).ToList();
        int totalSteps = Math.Max(1, filteredSteps.Count);
        details = $"profile={profile};steps={totalSteps}";

        foreach (var item in filteredSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            step++;
            yield return new CleanupStepUpdate
            {
                StepIndex = step,
                TotalSteps = totalSteps,
                Message = item.Start,
                IsCompleted = false
            };

            long freedBytes;
            try
            {
                freedBytes = await item.Action(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                outcome = "cancelled";
                details = $"cancelled_at_step={step};profile={profile}";
                RecordCleanupHistory(preview, outcome, totalFreedBytes, stopwatch, details);
                throw;
            }
            catch (Exception ex)
            {
                outcome = "failure";
                details = ex.Message;
                telemetryService.Increment(preview ? "cleanup_preview_failure_count" : "cleanup_manual_failure_count");
                logger.LogError("cleanup", "Cleanup failed.", ex);
                RecordCleanupHistory(preview, outcome, totalFreedBytes, stopwatch, details);
                throw;
            }

            totalFreedBytes += freedBytes;

            yield return new CleanupStepUpdate
            {
                StepIndex = step,
                TotalSteps = totalSteps,
                Message = item.End(freedBytes),
                IsCompleted = true,
                StepFreedBytes = freedBytes
            };
        }

        stopwatch.Stop();
        telemetryService.Increment(preview ? "cleanup_preview_success_count" : "cleanup_manual_success_count");
        telemetryService.AddToCounter(
            preview ? "cleanup_preview_estimated_mb_total" : "cleanup_manual_freed_mb_total",
            totalFreedBytes / (1024 * 1024));
        telemetryService.RecordDuration("cleanup_duration_ms", stopwatch.Elapsed);
        RecordCleanupHistory(preview, outcome, totalFreedBytes, stopwatch, details);
        logger.LogInfo("cleanup", $"Cleanup completed. preview={preview}, profile={profile}, bytes={totalFreedBytes}.");
    }

    private void RecordCleanupHistory(
        bool preview,
        string outcome,
        long totalFreedBytes,
        Stopwatch stopwatch,
        string details)
    {
        if (!stopwatch.IsRunning)
        {
            // already stopped
        }
        else
        {
            stopwatch.Stop();
        }

        historyService.AddEntry(new OperationHistoryEntry
        {
            OperationType = preview ? "cleanup_preview" : "cleanup_manual",
            Outcome = outcome,
            FreedBytes = totalFreedBytes,
            DurationMs = (long)stopwatch.Elapsed.TotalMilliseconds,
            Details = details
        });
    }

    public async Task<CleanupRunResult> RunBackgroundCleanupAsync(CleanupExecutionOptions options, CancellationToken cancellationToken = default)
    {
        long total = 0;
        await foreach (CleanupStepUpdate update in StreamCleanupAsync(options, cancellationToken))
        {
            if (update.IsCompleted)
            {
                total += update.StepFreedBytes;
            }
        }

        var result = new CleanupRunResult
        {
            IsPreview = options?.PreviewOnly ?? false,
            TotalFreedBytes = total
        };

        if (result.IsPreview)
        {
            result.PreviewItems = GetPreviewBreakdown(options?.ExclusionPatterns, options?.CleanupProfile).ToList();
        }

        return result;
    }

    public IReadOnlyList<CleanupPreviewItem> GetPreviewBreakdown(
        IReadOnlyList<string>? exclusionPatterns = null,
        string? cleanupProfile = null)
    {
        return BuildPreviewItems(
            exclusionPatterns?.ToList() ?? new List<string>(),
            CleanupProfiles.Normalize(cleanupProfile));
    }

    private static Task<long> RunOrEstimateAsync(bool preview, Func<long> execute, long estimated)
    {
        if (preview)
        {
            return Task.FromResult(estimated);
        }

        return Task.Run(execute);
    }

    private static double ToMb(long bytes) => Math.Round(bytes / 1024.0 / 1024.0, 2);

    internal static string FormatRegistryStepMessage(bool preview, int issueCount, RegistryCleanResult? result)
    {
        if (preview)
        {
            return issueCount == 0
                ? "✅ Registre : aucune entrée invalide détectée"
                : $"✅ Registre : {issueCount} entrée(s) à nettoyer détectée(s)";
        }

        if (result is null || issueCount == 0)
        {
            return "✅ Registre : aucune entrée invalide détectée";
        }

        string message = $"✅ Registre : {result.RemovedCount} entrée(s) nettoyée(s)";
        if (result.SkippedCount > 0)
        {
            message += $", {result.SkippedCount} ignorée(s)";
        }

        return result.BackupPath is null
            ? message
            : $"{message} · sauvegarde {Path.GetFileName(result.BackupPath)}";
    }

    private List<CleanupPreviewItem> BuildPreviewItems(List<string> exclusionPatterns, string profile)
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloadsPath = Path.Combine(userProfile, "Downloads");
        string tempPath = Path.GetTempPath();
        string windowsTemp = @"C:\Windows\Temp";
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string chromeCache = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache");
        string edgeCache = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache");
        string firefoxProfiles = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Mozilla\Firefox\Profiles");
        string thumbnails = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
        string logsPath = @"C:\Windows\Logs";
        string inetCache = Path.Combine(localAppData, @"Microsoft\Windows\INetCache");
        string deliveryCache = @"C:\Windows\SoftwareDistribution\DeliveryOptimization\Cache";
        string werRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Microsoft\Windows\WER");
        string defenderHistory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Microsoft\Windows Defender\Scans\History");
        string driverStoreTemp = @"C:\Windows\System32\DriverStore\Temp";

        var items = new List<(string CategoryKey, CleanupPreviewItem Item)>
        {
            (CleanupProfiles.CategoryTemp, new CleanupPreviewItem
            {
                Category = "Fichiers temporaires",
                Location = tempPath,
                EstimatedBytes = EstimateDirectoryBytes(tempPath) + EstimateDirectoryBytes(windowsTemp),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryTemp)
            }),
            (CleanupProfiles.CategoryInetCache, new CleanupPreviewItem
            {
                Category = "Fichiers Internet temporaires",
                Location = inetCache,
                EstimatedBytes = EstimateDirectoryBytes(inetCache),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryInetCache)
            }),
            (CleanupProfiles.CategoryBrowser, new CleanupPreviewItem
            {
                Category = "Cache Chrome",
                Location = chromeCache,
                EstimatedBytes = EstimateDirectoryBytes(chromeCache),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryBrowser)
            }),
            (CleanupProfiles.CategoryBrowser, new CleanupPreviewItem
            {
                Category = "Cache Edge",
                Location = edgeCache,
                EstimatedBytes = EstimateDirectoryBytes(edgeCache),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryBrowser)
            }),
            (CleanupProfiles.CategoryBrowser, new CleanupPreviewItem
            {
                Category = "Cache Firefox",
                Location = firefoxProfiles,
                EstimatedBytes = EstimateFirefoxCacheBytes(firefoxProfiles),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryBrowser)
            }),
            (CleanupProfiles.CategoryDeliveryOptimization, new CleanupPreviewItem
            {
                Category = "Delivery Optimization",
                Location = deliveryCache,
                EstimatedBytes = EstimateDirectoryBytes(deliveryCache),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryDeliveryOptimization)
            }),
            (CleanupProfiles.CategoryErrorReports, new CleanupPreviewItem
            {
                Category = "Rapports d'erreurs Windows",
                Location = werRoot,
                EstimatedBytes = EstimateDirectoryBytes(werRoot),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryErrorReports)
            }),
            (CleanupProfiles.CategoryDefender, new CleanupPreviewItem
            {
                Category = "Microsoft Defender",
                Location = defenderHistory,
                EstimatedBytes = EstimateDirectoryBytes(defenderHistory),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryDefender)
            }),
            (CleanupProfiles.CategoryDrivers, new CleanupPreviewItem
            {
                Category = "Composants Windows remplacés (DISM)",
                Location = @"C:\Windows\System32\DriverStore",
                EstimatedBytes = EstimateDirectoryBytes(driverStoreTemp),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryDrivers)
            }),
            (CleanupProfiles.CategoryDownloads, new CleanupPreviewItem
            {
                Category = "Téléchargements anciens",
                Location = downloadsPath,
                EstimatedBytes = EstimateOldDownloads(exclusionPatterns),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryDownloads)
            }),
            (CleanupProfiles.CategoryLogs, new CleanupPreviewItem
            {
                Category = "Logs Windows",
                Location = logsPath,
                EstimatedBytes = EstimateDirectoryBytes(logsPath) + EstimateDirectoryBytes(@"C:\Windows\System32\LogFiles"),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryLogs)
            }),
            (CleanupProfiles.CategoryThumbnails, new CleanupPreviewItem
            {
                Category = "Cache miniatures",
                Location = thumbnails,
                EstimatedBytes = EstimateThumbnailBytes(thumbnails),
                RiskLevel = CleanupProfiles.GetRiskLevel(profile, CleanupProfiles.CategoryThumbnails)
            })
        };

        if (CleanupProfiles.IncludesCategory(profile, CleanupProfiles.CategoryRegistry))
        {
            IEnumerable<CleanupPreviewItem> registryItems = registryCleaner
                .Scan(profile, exclusionPatterns)
                .GroupBy(issue => issue.Category)
                .Select(group => new CleanupPreviewItem
                {
                    Category = $"Registre · {RegistryIssueCategories.GetLabel(group.Key)}",
                    Location = string.Join(" | ", group.Select(i => i.Description).Distinct().Take(3)),
                    ItemCount = group.Count(),
                    RiskLevel = group.Any(i => i.RiskLevel == "Medium") ? "Medium" : "Low"
                });

            items.AddRange(registryItems.Select(item => (CleanupProfiles.CategoryRegistry, item)));
        }

        return items
            .Where(entry => CleanupProfiles.IncludesCategory(profile, entry.CategoryKey))
            .Select(entry => entry.Item)
            .ToList();
    }

    internal static long EstimateDirectoryBytes(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            return new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file =>
                {
                    try { return file.Length; }
                    catch { return 0L; }
                });
        }
        catch
        {
            return 0;
        }
    }

    private static long EstimateFirefoxCacheBytes(string profilesPath)
    {
        try
        {
            if (!Directory.Exists(profilesPath))
            {
                return 0;
            }

            return Directory.EnumerateDirectories(profilesPath)
                .Sum(profile => EstimateDirectoryBytes(Path.Combine(profile, "cache2")));
        }
        catch
        {
            return 0;
        }
    }

    private static long EstimateThumbnailBytes(string thumbnailsPath)
    {
        try
        {
            if (!Directory.Exists(thumbnailsPath))
            {
                return 0;
            }

            return new DirectoryInfo(thumbnailsPath)
                .EnumerateFiles("*cache*.db")
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    internal static long EstimateOldDownloads(IReadOnlyCollection<string> exclusionPatterns)
    {
        try
        {
            string downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            if (!Directory.Exists(downloadsPath))
            {
                return 0;
            }

            DateTime threshold = DateTime.Now.AddDays(-14);
            long total = 0;
            foreach (FileInfo file in new DirectoryInfo(downloadsPath).GetFiles())
            {
                if ((file.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                     file.Extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)) &&
                    file.LastWriteTime < threshold &&
                    !IsExcluded(file.FullName, exclusionPatterns))
                {
                    total += file.Length;
                }
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

    internal static bool IsExcluded(string candidatePath, IReadOnlyCollection<string> patterns)
    {
        if (patterns.Count == 0)
        {
            return false;
        }

        return patterns.Any(pattern =>
            !string.IsNullOrWhiteSpace(pattern) &&
            candidatePath.Contains(pattern.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
