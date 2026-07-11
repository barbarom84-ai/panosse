using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

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
        long size = 0;
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        size += CleanDirectory(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache"));
        size += CleanDirectory(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"));
        size += CleanDirectory(Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Code Cache"));
        return size;
    }

    public long CleanEdgeCache()
    {
        long size = 0;
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        size += CleanDirectory(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache"));
        size += CleanDirectory(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"));
        size += CleanDirectory(Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Code Cache"));
        return size;
    }

    public void CleanRegistry()
    {
        try
        {
            using RegistryKey? runMru = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", true);
            if (runMru != null)
            {
                foreach (string valueName in runMru.GetValueNames())
                {
                    if (!string.IsNullOrEmpty(valueName))
                    {
                        try { runMru.DeleteValue(valueName, false); } catch { }
                    }
                }
            }
        }
        catch { }

        try
        {
            using RegistryKey? recentDocs = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs", true);
            if (recentDocs == null)
            {
                return;
            }

            foreach (string valueName in recentDocs.GetValueNames())
            {
                if (!string.IsNullOrEmpty(valueName))
                {
                    try { recentDocs.DeleteValue(valueName, false); } catch { }
                }
            }

            foreach (string subKeyName in recentDocs.GetSubKeyNames())
            {
                try
                {
                    using RegistryKey? subKey = recentDocs.OpenSubKey(subKeyName, true);
                    if (subKey == null)
                    {
                        continue;
                    }

                    foreach (string valueName in subKey.GetValueNames())
                    {
                        if (!string.IsNullOrEmpty(valueName))
                        {
                            try { subKey.DeleteValue(valueName, false); } catch { }
                        }
                    }
                }
                catch { }
            }
        }
        catch { }
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

                    deletedSize += file.Length;
                    file.Delete();
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
        try
        {
            const string logsPath = @"C:\Windows\Logs";
            if (!Directory.Exists(logsPath))
            {
                return 0;
            }

            DirectoryInfo logsDir = new DirectoryInfo(logsPath);
            DateTime threshold = DateTime.Now.AddDays(-7);

            foreach (FileInfo file in logsDir.GetFiles("*.log", SearchOption.AllDirectories))
            {
                try
                {
                    if (file.LastWriteTime < threshold)
                    {
                        deletedSize += file.Length;
                        file.Delete();
                    }
                }
                catch { }
            }

            foreach (FileInfo file in logsDir.GetFiles("*.etl", SearchOption.AllDirectories))
            {
                try
                {
                    if (file.LastWriteTime < threshold)
                    {
                        deletedSize += file.Length;
                        file.Delete();
                    }
                }
                catch { }
            }

            foreach (FileInfo file in logsDir.GetFiles("*.old", SearchOption.AllDirectories))
            {
                try
                {
                    deletedSize += file.Length;
                    file.Delete();
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
                    deletedSize += file.Length;
                    file.Delete();
                }
                catch { }
            }

            foreach (FileInfo file in thumbnailsDir.GetFiles("iconcache*.db"))
            {
                try
                {
                    deletedSize += file.Length;
                    file.Delete();
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
                    deletedSize += file.Length;
                    file.Delete();
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
                    deletedSize += file.Length;
                    file.Delete();
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
