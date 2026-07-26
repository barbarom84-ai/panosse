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
    private readonly ITelemetryService telemetryService;
    private readonly IOperationHistoryService historyService;
    private readonly ILoggerService logger;

    public CleanupOrchestrator(
        ICleanupService cleanupService,
        ITelemetryService telemetryService,
        IOperationHistoryService historyService,
        ILoggerService logger)
    {
        this.cleanupService = cleanupService;
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
        int totalSteps = 12;
        int step = 0;

        var stopwatch = Stopwatch.StartNew();
        long totalFreedBytes = 0;

        telemetryService.Increment(preview ? "cleanup_preview_start_count" : "cleanup_manual_start_count");

        var steps = new List<(string Start, Func<CancellationToken, Task<long>> Action, Func<long, string> End)>
        {
            ("🗑️ Vidage de la corbeille...", ct => RunOrEstimateAsync(preview, () => { cleanupService.EmptyRecycleBinAsync().GetAwaiter().GetResult(); return 0L; }, 0), _ => "✅ Corbeille traitée"),
            ("🧹 Nettoyage des fichiers temporaires...", _ => RunOrEstimateAsync(preview, cleanupService.CleanTemporaryFiles, 0), b => $"✅ Fichiers temporaires traités ({ToMb(b)} Mo)"),
            ("🌐 Nettoyage du cache Chrome...", _ => RunOrEstimateAsync(preview, cleanupService.CleanChromeCache, 0), b => $"✅ Cache Chrome traité ({ToMb(b)} Mo)"),
            ("🌐 Nettoyage du cache Edge...", _ => RunOrEstimateAsync(preview, cleanupService.CleanEdgeCache, 0), b => $"✅ Cache Edge traité ({ToMb(b)} Mo)"),
            ("🌐 Nettoyage du cache Firefox...", _ => RunOrEstimateAsync(preview, cleanupService.CleanFirefoxCache, 0), b => $"✅ Cache Firefox traité ({ToMb(b)} Mo)"),
            ("🌐 Nettoyage du cache Opera...", _ => RunOrEstimateAsync(preview, cleanupService.CleanOperaCache, 0), b => $"✅ Cache Opera traité ({ToMb(b)} Mo)"),
            ("🌐 Nettoyage du cache Brave...", _ => RunOrEstimateAsync(preview, cleanupService.CleanBraveCache, 0), b => $"✅ Cache Brave traité ({ToMb(b)} Mo)"),
            ("🌐 Nettoyage du cache Vivaldi...", _ => RunOrEstimateAsync(preview, cleanupService.CleanVivaldiCache, 0), b => $"✅ Cache Vivaldi traité ({ToMb(b)} Mo)"),
            ("📋 Nettoyage du registre...", _ => RunOrEstimateAsync(preview, () => { cleanupService.CleanRegistry(); return 0L; }, 0), _ => "✅ Registre traité"),
            ("📥 Nettoyage des téléchargements anciens...", _ => RunOrEstimateAsync(preview, () => cleanupService.CleanOldDownloads(options.ExclusionPatterns), EstimateOldDownloads(options.ExclusionPatterns)), b => $"✅ Téléchargements traités ({ToMb(b)} Mo)"),
            ("📄 Nettoyage des logs Windows...", _ => RunOrEstimateAsync(preview, cleanupService.CleanWindowsLogs, 0), b => $"✅ Logs Windows traités ({ToMb(b)} Mo)"),
            ("🖼️ Nettoyage du cache des miniatures...", _ => RunOrEstimateAsync(preview, cleanupService.CleanThumbnailCache, 0), b => $"✅ Cache miniatures traité ({ToMb(b)} Mo)")
        };

        foreach (var item in steps)
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

            long freedBytes = await item.Action(cancellationToken);
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
        telemetryService.AddToCounter(preview ? "cleanup_preview_estimated_mb_total" : "cleanup_manual_freed_mb_total", totalFreedBytes / (1024 * 1024));
        telemetryService.RecordDuration("cleanup_duration_ms", stopwatch.Elapsed);
        historyService.AddEntry(new OperationHistoryEntry
        {
            OperationType = preview ? "cleanup_preview" : "cleanup_manual",
            Outcome = "success",
            FreedBytes = totalFreedBytes,
            DurationMs = (long)stopwatch.Elapsed.TotalMilliseconds,
            Details = $"steps={totalSteps}"
        });
        logger.LogInfo("cleanup", $"Cleanup completed. preview={preview}, bytes={totalFreedBytes}.");
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
            result.PreviewItems = BuildPreviewItems(options?.ExclusionPatterns ?? new List<string>());
        }

        return result;
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

    private List<CleanupPreviewItem> BuildPreviewItems(List<string> exclusionPatterns)
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloadsPath = Path.Combine(userProfile, "Downloads");
        string tempPath = Path.GetTempPath();
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return new List<CleanupPreviewItem>
        {
            new()
            {
                Category = "Temporary files",
                Location = tempPath,
                EstimatedBytes = EstimateTopLevelBytes(tempPath),
                RiskLevel = "Low"
            },
            new()
            {
                Category = "Old downloads",
                Location = downloadsPath,
                EstimatedBytes = EstimateOldDownloads(exclusionPatterns),
                RiskLevel = "Medium"
            },
            new()
            {
                Category = "Browser cache",
                Location = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"),
                EstimatedBytes = EstimateTopLevelBytes(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache")),
                RiskLevel = "Low"
            }
        };
    }

    private static long EstimateTopLevelBytes(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            return new DirectoryInfo(path)
                .GetFiles()
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }

    private static long EstimateOldDownloads(IReadOnlyCollection<string> exclusionPatterns)
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

    private static bool IsExcluded(string candidatePath, IReadOnlyCollection<string> patterns)
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
