using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Panosse.Services;

public enum RegistryPathState
{
    Exists,
    Missing,

    /// <summary>Impossible de conclure (réseau, amovible, accès refusé, dossier Windows…) : ne jamais supprimer.</summary>
    Unknown
}

/// <summary>
/// Extraction et vérification prudente des chemins référencés par le registre.
/// Toute ambiguïté se traduit par <see cref="RegistryPathState.Unknown"/>.
/// </summary>
internal static class RegistryPathInspector
{
    private static readonly string[] ExecutableExtensions =
    [
        ".exe", ".com", ".bat", ".cmd", ".lnk", ".dll", ".scr", ".cpl", ".msc", ".vbs", ".js", ".ps1"
    ];

    private static readonly string[] MuiCacheSuffixes = [".FriendlyAppName", ".ApplicationCompany"];

    public static bool TryGetExecutablePath(string? commandLine, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return false;
        }

        string expanded = Environment.ExpandEnvironmentVariables(commandLine.Trim());
        if (expanded.Contains('%'))
        {
            return false;
        }

        string candidate;
        if (expanded.StartsWith('"'))
        {
            int closing = expanded.IndexOf('"', 1);
            if (closing <= 1)
            {
                return false;
            }

            candidate = expanded[1..closing];
        }
        else if (!expanded.Contains(' '))
        {
            candidate = expanded;
        }
        else if (!TryFindUnquotedExecutable(expanded, out candidate))
        {
            return false;
        }

        if (!IsLocalAbsolutePath(candidate))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    public static bool TryParseMuiCacheValueName(string? valueName, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(valueName) || valueName.StartsWith('@'))
        {
            return false;
        }

        string? suffix = MuiCacheSuffixes.FirstOrDefault(s => valueName.EndsWith(s, StringComparison.OrdinalIgnoreCase));
        if (suffix is null)
        {
            return false;
        }

        string candidate = valueName[..^suffix.Length];
        if (!IsLocalAbsolutePath(candidate))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    public static RegistryPathState GetPathState(string path)
    {
        try
        {
            if (!IsLocalAbsolutePath(path))
            {
                return RegistryPathState.Unknown;
            }

            string fullPath = Path.GetFullPath(path);
            if (IsUnderProtectedRoot(fullPath))
            {
                return RegistryPathState.Unknown;
            }

            var drive = new DriveInfo(Path.GetPathRoot(fullPath)!);
            if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
            {
                return RegistryPathState.Unknown;
            }

            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                return RegistryPathState.Exists;
            }

            string? ancestor = Path.GetDirectoryName(fullPath);
            while (ancestor is not null && !Directory.Exists(ancestor))
            {
                ancestor = Path.GetDirectoryName(ancestor);
            }

            if (ancestor is null)
            {
                return RegistryPathState.Unknown;
            }

            // File.Exists renvoie false sur accès refusé : on exige de pouvoir lister l'ancêtre.
            using IEnumerator<string> probe = Directory.EnumerateFileSystemEntries(ancestor).GetEnumerator();
            _ = probe.MoveNext();
            return RegistryPathState.Missing;
        }
        catch
        {
            return RegistryPathState.Unknown;
        }
    }

    private static bool TryFindUnquotedExecutable(string commandLine, out string candidate)
    {
        candidate = string.Empty;
        int searchFrom = 0;
        while (searchFrom <= commandLine.Length)
        {
            int space = commandLine.IndexOf(' ', searchFrom);
            string prefix = space < 0 ? commandLine : commandLine[..space];
            if (ExecutableExtensions.Any(ext => prefix.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                candidate = prefix;
                return true;
            }

            if (space < 0)
            {
                break;
            }

            searchFrom = space + 1;
        }

        return false;
    }

    private static bool IsLocalAbsolutePath(string path) =>
        path.Length >= 3 &&
        char.IsAsciiLetter(path[0]) &&
        path[1] == ':' &&
        (path[2] == '\\' || path[2] == '/') &&
        path.IndexOfAny(Path.GetInvalidPathChars()) < 0;

    private static bool IsUnderProtectedRoot(string fullPath)
    {
        foreach (string root in GetProtectedRoots())
        {
            if (!string.IsNullOrEmpty(root) &&
                fullPath.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetProtectedRoots()
    {
        // Dossier Windows : redirection WOW64 et fichiers système → jamais de verdict « manquant ».
        yield return Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string? programW6432 = Environment.GetEnvironmentVariable("ProgramW6432");
        foreach (string? root in new[] { programFiles, programFilesX86, programW6432 })
        {
            if (!string.IsNullOrEmpty(root))
            {
                yield return Path.Combine(root, "WindowsApps");
            }
        }
    }
}
