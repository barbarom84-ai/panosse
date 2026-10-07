using System;
using System.Collections.Generic;
using System.IO;

namespace Panosse.Services;

/// <summary>
/// Emplacements partagés entre le nettoyage (<see cref="CleanupService"/>) et l'estimation de l'aperçu.
/// </summary>
public static class CleanupPaths
{
    public const int WindowsUpdateDownloadMinimumAgeDays = 7;

    private static string LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static string WindowsDirectory => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    private static string SystemDrive => Path.GetPathRoot(WindowsDirectory) ?? @"C:\";
    private static string LocalLow => Path.Combine(UserProfile, "AppData", "LocalLow");

    public static IReadOnlyList<string> ShaderCacheDirectories() =>
    [
        Path.Combine(LocalAppData, "D3DSCache"),
        Path.Combine(LocalAppData, @"NVIDIA\DXCache"),
        Path.Combine(LocalAppData, @"NVIDIA\GLCache"),
        Path.Combine(LocalLow, @"NVIDIA\PerDriverVersion\DXCache"),
        Path.Combine(LocalLow, @"NVIDIA\PerDriverVersion\GLCache"),
        Path.Combine(LocalAppData, @"AMD\DxCache"),
        Path.Combine(LocalAppData, @"AMD\DxcCache"),
        Path.Combine(LocalAppData, @"AMD\GLCache"),
        Path.Combine(LocalAppData, @"AMD\VkCache"),
        Path.Combine(LocalLow, @"Intel\ShaderCache")
    ];

    public static string MinidumpDirectory => Path.Combine(WindowsDirectory, "Minidump");

    public static string MemoryDumpFile => Path.Combine(WindowsDirectory, "MEMORY.DMP");

    public static string WindowsUpdateDownloadDirectory => Path.Combine(WindowsDirectory, @"SoftwareDistribution\Download");

    public static IReadOnlyList<string> PreviousWindowsDirectories() =>
    [
        Path.Combine(SystemDrive, "Windows.old"),
        Path.Combine(SystemDrive, "$WINDOWS.~BT"),
        Path.Combine(SystemDrive, "$WINDOWS.~WS")
    ];

    /// <summary>
    /// Caches de gestionnaires de paquets : retéléchargés automatiquement au prochain build / install.
    /// </summary>
    public static IReadOnlyList<string> DeveloperCacheDirectories()
    {
        var paths = new List<string>
        {
            Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } nugetPackages
                ? nugetPackages
                : Path.Combine(UserProfile, @".nuget\packages"),
            Path.Combine(LocalAppData, @"NuGet\v3-cache"),
            Path.Combine(LocalAppData, @"NuGet\http-cache"),
            Path.Combine(LocalAppData, @"NuGet\plugins-cache"),
            Path.Combine(LocalAppData, "npm-cache"),
            Path.Combine(LocalAppData, @"pip\Cache"),
            Path.Combine(LocalAppData, @"Yarn\Cache"),
            Path.Combine(LocalAppData, "pnpm-cache")
        };

        string gradleHome = Environment.GetEnvironmentVariable("GRADLE_USER_HOME") is { Length: > 0 } customGradle
            ? customGradle
            : Path.Combine(UserProfile, ".gradle");
        paths.Add(Path.Combine(gradleHome, "caches"));

        return paths;
    }

    public static long EstimateDirectories(IEnumerable<string> directories)
    {
        long total = 0;
        foreach (string directory in directories)
        {
            total += EstimateDirectoryBytes(directory);
        }

        return total;
    }

    public static long EstimateDirectoryBytes(string path, int? olderThanDays = null)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            DateTime? threshold = olderThanDays is int days ? DateTime.Now.AddDays(-days) : null;
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            long total = 0;
            foreach (FileInfo file in new DirectoryInfo(path).EnumerateFiles("*", options))
            {
                try
                {
                    if (threshold is null || file.LastWriteTime < threshold)
                    {
                        total += file.Length;
                    }
                }
                catch
                {
                    // Fichier disparu ou inaccessible pendant l'énumération.
                }
            }

            return total;
        }
        catch
        {
            return 0;
        }
    }

    public static long GetFileLength(string path)
    {
        try
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(path) ?? string.Empty);
            foreach (FileInfo file in directory.EnumerateFiles(Path.GetFileName(path)))
            {
                return file.Length;
            }
        }
        catch
        {
            // Dossier inaccessible.
        }

        return 0;
    }
}
