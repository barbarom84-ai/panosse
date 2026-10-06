using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class CleanupService : ICleanupService
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string rootPath, uint flags);

    public async Task EmptyRecycleBinAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                SHEmptyRecycleBin(IntPtr.Zero, string.Empty, 1 | 2 | 4);
            }
            catch
            {
                // Ignorer les erreurs de la corbeille.
            }
        });
    }

    public long CleanTemporaryFiles()
    {
        long size = 0;
        size += CleanDirectory(Path.GetTempPath());
        size += CleanDirectory(@"C:\Windows\Temp");
        return size;
    }

    public long CleanChromeCache()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return CleanChromiumProfiles(Path.Combine(localAppData, @"Google\Chrome\User Data"));
    }

    public long CleanEdgeCache()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return CleanChromiumProfiles(Path.Combine(localAppData, @"Microsoft\Edge\User Data"));
    }

    public long CleanFirefoxCache()
    {
        long size = 0;
        string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        size += CleanFirefoxProfiles(Path.Combine(roamingAppData, @"Mozilla\Firefox\Profiles"));
        size += CleanFirefoxProfiles(Path.Combine(localAppData, @"Mozilla\Firefox\Profiles"));
        return size;
    }

    public long CleanOperaCache()
    {
        long size = 0;
        string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        size += CleanOperaProfile(Path.Combine(roamingAppData, @"Opera Software\Opera Stable"));
        size += CleanOperaProfile(Path.Combine(roamingAppData, @"Opera Software\Opera GX Stable"));
        return size;
    }

    public long CleanBraveCache()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return CleanChromiumProfiles(Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data"));
    }

    public long CleanVivaldiCache()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return CleanChromiumProfiles(Path.Combine(localAppData, @"Vivaldi\User Data"));
    }

    private long CleanChromiumProfiles(string userDataPath)
    {
        long size = 0;
        string[] cacheRelativePaths =
        [
            "Cache",
            "Code Cache",
            "GPUCache",
            Path.Combine("Service Worker", "CacheStorage"),
            Path.Combine("Service Worker", "ScriptCache"),
            "GrShaderCache",
            "ShaderCache"
        ];

        foreach (string profilePath in GetProfileDirectories(userDataPath))
        {
            foreach (string relative in cacheRelativePaths)
            {
                size += CleanDirectory(Path.Combine(profilePath, relative));
            }
        }

        return size;
    }

    private long CleanFirefoxProfiles(string profilesPath)
    {
        long size = 0;
        if (!Directory.Exists(profilesPath))
        {
            return size;
        }

        try
        {
            foreach (string profilePath in Directory.EnumerateDirectories(profilesPath))
            {
                size += CleanDirectory(Path.Combine(profilePath, "cache2"));
            }
        }
        catch
        {
            // Ignorer les profils absents ou inaccessibles.
        }

        return size;
    }

    private long CleanOperaProfile(string profilePath)
    {
        long size = 0;
        size += CleanDirectory(Path.Combine(profilePath, "Cache"));
        size += CleanDirectory(Path.Combine(profilePath, "Code Cache"));
        size += CleanDirectory(Path.Combine(profilePath, "GPUCache"));
        size += CleanChromiumProfiles(profilePath);
        return size;
    }

    private static IEnumerable<string> GetProfileDirectories(string userDataPath)
    {
        if (!Directory.Exists(userDataPath))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateDirectories(userDataPath)
                .Where(path =>
                {
                    string name = Path.GetFileName(path);
                    return name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                           name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    public long CleanOldDownloads(IEnumerable<string>? exclusionPatterns = null)
    {
        long deletedSize = 0;
        var patterns = exclusionPatterns?
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .ToList() ?? new List<string>();

        try
        {
            string downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads");

            if (!Directory.Exists(downloadsPath))
            {
                return 0;
            }

            DirectoryInfo downloadsDir = new DirectoryInfo(downloadsPath);
            DateTime deleteThreshold = DateTime.Now.AddDays(-14);

            var filesToDelete = downloadsDir.GetFiles()
                .Where(f =>
                    (f.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                     f.Extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)) &&
                    f.LastWriteTime < deleteThreshold);

            foreach (FileInfo file in filesToDelete)
            {
                try
                {
                    if (IsExcluded(file.FullName, patterns))
                    {
                        continue;
                    }

                    long size = file.Length;
                    file.Delete();
                    deletedSize += size;
                }
                catch
                {
                    // Ignorer les fichiers verrouillés/protégés.
                }
            }
        }
        catch
        {
            // Ignorer les erreurs d'accès.
        }

        return deletedSize;
    }

    private static bool IsExcluded(string candidatePath, IReadOnlyCollection<string> exclusionPatterns)
    {
        if (exclusionPatterns.Count == 0)
        {
            return false;
        }

        return exclusionPatterns.Any(pattern =>
            candidatePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    public long CleanWindowsLogs()
    {
        long deletedSize = 0;
        string[] logRoots =
        [
            @"C:\Windows\Logs",
            @"C:\Windows\System32\LogFiles",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Logs")
        ];

        foreach (string logsPath in logRoots)
        {
            deletedSize += CleanOldLogFiles(logsPath, olderThanDays: 7);
        }

        return deletedSize;
    }

    public long CleanThumbnailCache()
    {
        long deletedSize = 0;
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string thumbnailsPath = Path.Combine(localAppData, @"Microsoft\Windows\Explorer");
            if (!Directory.Exists(thumbnailsPath))
            {
                return 0;
            }

            DirectoryInfo thumbnailsDir = new DirectoryInfo(thumbnailsPath);
            foreach (FileInfo file in thumbnailsDir.GetFiles("thumbcache*.db"))
            {
                try
                {
                    long size = file.Length;
                    file.Delete();
                    deletedSize += size;
                }
                catch { }
            }

            foreach (FileInfo file in thumbnailsDir.GetFiles("iconcache*.db"))
            {
                try
                {
                    long size = file.Length;
                    file.Delete();
                    deletedSize += size;
                }
                catch { }
            }
        }
        catch
        {
            // Ignorer les erreurs d'accès.
        }

        return deletedSize;
    }

    public long CleanTemporaryInternetFiles()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        long size = 0;
        size += CleanDirectory(Path.Combine(localAppData, @"Microsoft\Windows\INetCache"));
        size += CleanDirectory(Path.Combine(localAppData, @"Microsoft\Windows\WebCache"));
        return size;
    }

    public long CleanDeliveryOptimization()
    {
        long size = 0;
        string[] cachePaths =
        [
            @"C:\Windows\SoftwareDistribution\DeliveryOptimization\Cache",
            @"C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"Microsoft\Windows\DeliveryOptimization\Cache")
        ];

        foreach (string path in cachePaths)
        {
            size += CleanDirectory(path);
        }

        return size;
    }

    public long CleanWindowsErrorReports()
    {
        long size = 0;
        string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] werPaths =
        [
            Path.Combine(programData, @"Microsoft\Windows\WER\ReportQueue"),
            Path.Combine(programData, @"Microsoft\Windows\WER\ReportArchive"),
            Path.Combine(programData, @"Microsoft\Windows\WER\Temp"),
            Path.Combine(localAppData, @"Microsoft\Windows\WER"),
            Path.Combine(localAppData, @"CrashDumps")
        ];

        foreach (string path in werPaths)
        {
            size += CleanDirectory(path);
        }

        return size;
    }

    public long CleanDefenderArtifacts()
    {
        long size = 0;
        string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string defenderRoot = Path.Combine(programData, @"Microsoft\Windows Defender");

        // Historique / support / quarantaine — pas les définitions actives.
        string[] paths =
        [
            Path.Combine(defenderRoot, @"Scans\History"),
            Path.Combine(defenderRoot, "Support"),
            Path.Combine(defenderRoot, "Quarantine"),
            Path.Combine(programData, @"Microsoft\Windows Defender Network Inspection\Support")
        ];

        foreach (string path in paths)
        {
            size += CleanDirectory(path);
        }

        return size;
    }

    public long CleanObsoleteDriverPackages()
    {
        // Temp du Driver Store (sûr) + nettoyage CBS des composants / pilotes remplacés via DISM.
        long size = CleanDirectory(@"C:\Windows\System32\DriverStore\Temp");
        size += RunDismStartComponentCleanup();
        return size;
    }

    private static long CleanOldLogFiles(string logsPath, int olderThanDays)
    {
        long deletedSize = 0;
        try
        {
            if (!Directory.Exists(logsPath))
            {
                return 0;
            }

            DirectoryInfo logsDir = new DirectoryInfo(logsPath);
            DateTime threshold = DateTime.Now.AddDays(-olderThanDays);

            foreach (string pattern in new[] { "*.log", "*.etl", "*.old" })
            {
                foreach (FileInfo file in logsDir.GetFiles(pattern, SearchOption.AllDirectories))
                {
                    try
                    {
                        bool delete = pattern == "*.old" || file.LastWriteTime < threshold;
                        if (!delete)
                        {
                            continue;
                        }

                        long length = file.Length;
                        file.Delete();
                        deletedSize += length;
                    }
                    catch { }
                }
            }
        }
        catch
        {
            // Ignorer les erreurs d'accès.
        }

        return deletedSize;
    }

    private static long RunDismStartComponentCleanup()
    {
        try
        {
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string dismPath = Path.Combine(windir, "System32", "dism.exe");
            if (!File.Exists(dismPath))
            {
                return 0;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = dismPath,
                Arguments = "/Online /Cleanup-Image /StartComponentCleanup",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return 0;
            }

            // DISM peut prendre plusieurs minutes ; ne pas bloquer indéfiniment.
            if (!process.WaitForExit(milliseconds: 8 * 60 * 1000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return 0;
            }

            // Les octets libérés ne sont pas exposés de façon fiable par DISM.
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private long CleanDirectory(string path)
    {
        long deletedSize = 0;
        if (!Directory.Exists(path))
        {
            return 0;
        }

        try
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    long size = file.Length;
                    file.Delete();
                    deletedSize += size;
                }
                catch { }
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                try
                {
                    deletedSize += CleanDirectoryRecursive(subDir);
                }
                catch { }
            }
        }
        catch { }

        return deletedSize;
    }

    private long CleanDirectoryRecursive(DirectoryInfo directory)
    {
        long deletedSize = 0;
        try
        {
            foreach (FileInfo file in directory.GetFiles())
            {
                try
                {
                    long size = file.Length;
                    file.Delete();
                    deletedSize += size;
                }
                catch { }
            }

            foreach (DirectoryInfo subDir in directory.GetDirectories())
            {
                try
                {
                    deletedSize += CleanDirectoryRecursive(subDir);
                    subDir.Delete();
                }
                catch { }
            }
        }
        catch { }

        return deletedSize;
    }
}
