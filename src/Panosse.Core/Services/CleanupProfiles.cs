using System;

namespace Panosse.Services;

/// <summary>
/// Cleanup intensity profiles shared by orchestration, preview, and settings.
/// </summary>
public static class CleanupProfiles
{
    public const string Rapid = "Rapid";
    public const string Standard = "Standard";
    public const string Deep = "Deep";

    public const string CategoryRecycle = "recycle";
    public const string CategoryTemp = "temp";
    public const string CategoryBrowser = "browser";
    public const string CategoryRegistry = "registry";
    public const string CategoryDownloads = "downloads";
    public const string CategoryLogs = "logs";
    public const string CategoryThumbnails = "thumbnails";
    public const string CategoryInetCache = "inetcache";
    public const string CategoryDeliveryOptimization = "delivery";
    public const string CategoryErrorReports = "wer";
    public const string CategoryDefender = "defender";
    public const string CategoryDrivers = "drivers";
    public const string CategoryShaders = "shaders";
    public const string CategoryCrashDumps = "dumps";
    public const string CategoryWindowsUpdate = "wudownload";
    public const string CategoryPreviousWindows = "windowsold";
    public const string CategoryDevCaches = "devcaches";

    public static string Normalize(string? profile)
    {
        if (string.Equals(profile, Rapid, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profile, "Rapide", StringComparison.OrdinalIgnoreCase))
        {
            return Rapid;
        }

        if (string.Equals(profile, Deep, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(profile, "Profond", StringComparison.OrdinalIgnoreCase))
        {
            return Deep;
        }

        return Standard;
    }

    public static bool IncludesCategory(string? profile, string category)
    {
        string normalized = Normalize(profile);
        string key = category?.Trim().ToLowerInvariant() ?? string.Empty;

        return normalized switch
        {
            Rapid => key is CategoryRecycle or CategoryTemp or CategoryThumbnails,
            // DISM, Windows.old (pas de retour arrière) et caches dev (retéléchargement lourd) → Profond uniquement.
            Standard => key is not (CategoryDrivers or CategoryPreviousWindows or CategoryDevCaches),
            _ => true
        };
    }

    public static string GetRiskLevel(string? profile, string category)
    {
        string normalized = Normalize(profile);
        string key = category?.Trim().ToLowerInvariant() ?? string.Empty;

        if (key is CategoryDrivers or CategoryPreviousWindows)
        {
            return "High";
        }

        if (key is CategoryDevCaches)
        {
            return "Medium";
        }

        if (normalized == Deep &&
            key is CategoryRegistry or CategoryDownloads or CategoryLogs or CategoryDefender)
        {
            return "High";
        }

        if (key is CategoryDownloads or CategoryDefender)
        {
            return "Medium";
        }

        return "Low";
    }

    public static string GetDisplayName(string? profile) =>
        Normalize(profile) switch
        {
            Rapid => "Rapide",
            Deep => "Profond",
            _ => "Standard"
        };

    public static string GetDescription(string? profile) =>
        Normalize(profile) switch
        {
            Rapid => "Corbeille, temporaires et miniatures uniquement.",
            Deep => "Nettoyage complet + registre système, composants Windows remplacés (DISM), ancienne installation de Windows (Windows.old) et caches développeur (NuGet, npm, pip, Gradle…). Registre sauvegardé avant modification.",
            _ => "Nettoyage complet (navigateurs, logs, Windows Update, shaders GPU, dumps, Defender, WER, registre utilisateur sauvegardé…)."
        };

    public static int ToIndex(string? profile) =>
        Normalize(profile) switch
        {
            Rapid => 0,
            Deep => 2,
            _ => 1
        };

    public static string FromIndex(int index) =>
        index switch
        {
            0 => Rapid,
            2 => Deep,
            _ => Standard
        };
}
