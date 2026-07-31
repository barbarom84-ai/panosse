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
            _ => true
        };
    }

    public static string GetRiskLevel(string? profile, string category)
    {
        string normalized = Normalize(profile);
        string key = category?.Trim().ToLowerInvariant() ?? string.Empty;

        if (normalized == Deep &&
            key is CategoryRegistry or CategoryDownloads or CategoryLogs)
        {
            return "High";
        }

        if (key is CategoryDownloads)
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
            Deep => "Nettoyage complet. Registre, téléchargements et logs : risque élevé.",
            _ => "Nettoyage complet (navigateurs, logs, téléchargements anciens)."
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
