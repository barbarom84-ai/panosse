using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Panosse.Services;

public sealed class RegistryCleanerService : IRegistryCleanerService
{
    private const int MaxBackups = 30;
    private const string RegHeader = "Windows Registry Editor Version 5.00";
    private const string Explorer = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunKeyWow = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedRun = Explorer + @"\StartupApproved\Run";
    private const string StartupApprovedRun32 = Explorer + @"\StartupApproved\Run32";
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string UninstallKeyWow = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string AppPathsKey = @"Software\Microsoft\Windows\CurrentVersion\App Paths";
    private const string MuiCacheKey = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";
    private const string CompatStoreKey = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store";
    private const string CompatLayersKey = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
    private const string SharedDllsKey = @"Software\Microsoft\Windows\CurrentVersion\SharedDLLs";
    private const string SharedDllsKeyWow = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs";

    private static readonly (string KeyPath, string Description)[] PrivacyKeys =
    [
        (Explorer + @"\RunMRU", "Historique de la boîte Exécuter (Win+R)"),
        (Explorer + @"\RecentDocs", "Documents récents de l'Explorateur"),
        (Explorer + @"\TypedPaths", "Chemins tapés dans l'Explorateur"),
        (Explorer + @"\WordWheelQuery", "Recherches de l'Explorateur"),
        (Explorer + @"\ComDlg32\OpenSavePidlMRU", "Fichiers récents des boîtes Ouvrir/Enregistrer"),
        (Explorer + @"\ComDlg32\LastVisitedPidlMRU", "Dossiers récents des boîtes Ouvrir/Enregistrer")
    ];

    /// <summary>Seules ces racines peuvent être modifiées, quoi que contienne la liste d'entrées.</summary>
    private static readonly string[] DefaultAllowedRoots =
    [
        .. PrivacyKeys.Select(k => k.KeyPath),
        RunKey, RunKeyWow, StartupApprovedRun, StartupApprovedRun32,
        UninstallKey, UninstallKeyWow, AppPathsKey, MuiCacheKey,
        CompatStoreKey, CompatLayersKey, SharedDllsKey, SharedDllsKeyWow
    ];

    private readonly ILoggerService logger;
    private readonly Func<bool> isElevated;
    private readonly string[] allowedRoots;

    public RegistryCleanerService(ILoggerService logger)
        : this(logger, DefaultBackupDirectory(), IsProcessElevated)
    {
    }

    internal RegistryCleanerService(
        ILoggerService logger,
        string backupDirectory,
        Func<bool> isElevated,
        IEnumerable<string>? extraAllowedRoots = null)
    {
        this.logger = logger;
        this.isElevated = isElevated;
        BackupDirectory = backupDirectory;
        allowedRoots = [.. DefaultAllowedRoots, .. extraAllowedRoots ?? []];
    }

    public string BackupDirectory { get; }

    public IReadOnlyList<RegistryIssue> Scan(
        string? cleanupProfile,
        IReadOnlyCollection<string>? exclusionPatterns = null,
        CancellationToken cancellationToken = default)
    {
        string profile = CleanupProfiles.Normalize(cleanupProfile);
        if (!CleanupProfiles.IncludesCategory(profile, CleanupProfiles.CategoryRegistry))
        {
            return [];
        }

        var issues = new List<RegistryIssue>();
        var pathCache = new Dictionary<string, RegistryPathState>(StringComparer.OrdinalIgnoreCase);
        bool machineWritable = isElevated();

        void Collect(string name, Action scanner)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                scanner();
            }
            catch (Exception ex)
            {
                logger.LogWarning("registry", $"Scan '{name}' interrompu : {ex.Message}");
            }
        }

        Collect("privacy", () => ScanPrivacy(issues));
        Collect("run-hkcu", () => ScanRunKey(issues, pathCache, RegistryHive.CurrentUser, RunKey, StartupApprovedRun, "Low"));
        Collect("apppaths-hkcu", () => ScanAppPaths(issues, pathCache, RegistryHive.CurrentUser, "Low"));
        Collect("muicache", () => ScanMuiCache(issues, pathCache));
        Collect("compat-store", () => ScanPathNamedValues(issues, pathCache, RegistryHive.CurrentUser, CompatStoreKey, RegistryIssueCategories.Compatibility, "Low"));
        Collect("compat-layers", () => ScanPathNamedValues(issues, pathCache, RegistryHive.CurrentUser, CompatLayersKey, RegistryIssueCategories.Compatibility, "Low"));
        Collect("uninstall-hkcu", () => ScanUninstall(issues, pathCache, RegistryHive.CurrentUser, UninstallKey));

        if (machineWritable)
        {
            Collect("run-hklm", () => ScanRunKey(issues, pathCache, RegistryHive.LocalMachine, RunKey, StartupApprovedRun, "Medium"));
            Collect("run-hklm-wow", () => ScanRunKey(issues, pathCache, RegistryHive.LocalMachine, RunKeyWow, StartupApprovedRun32, "Medium"));
            Collect("apppaths-hklm", () => ScanAppPaths(issues, pathCache, RegistryHive.LocalMachine, "Medium"));
            Collect("uninstall-hklm", () => ScanUninstall(issues, pathCache, RegistryHive.LocalMachine, UninstallKey));
            Collect("uninstall-hklm-wow", () => ScanUninstall(issues, pathCache, RegistryHive.LocalMachine, UninstallKeyWow));
            Collect("shareddlls", () => ScanPathNamedValues(issues, pathCache, RegistryHive.LocalMachine, SharedDllsKey, RegistryIssueCategories.SharedDlls, "Medium"));
            Collect("shareddlls-wow", () => ScanPathNamedValues(issues, pathCache, RegistryHive.LocalMachine, SharedDllsKeyWow, RegistryIssueCategories.SharedDlls, "Medium"));
        }

        bool includeMedium = profile == CleanupProfiles.Deep;
        IReadOnlyCollection<string> patterns = exclusionPatterns ?? [];
        return issues
            .Where(i => includeMedium || i.RiskLevel == "Low")
            .Where(i => !IsIssueExcluded(i, patterns))
            .ToList();
    }

    public RegistryCleanResult Clean(IReadOnlyList<RegistryIssue> issues, CancellationToken cancellationToken = default)
    {
        if (issues.Count == 0)
        {
            return new RegistryCleanResult();
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "panosse-reg-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDirectory);
            var exportedKeys = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var exportedContents = new List<string>();

            foreach (RegistryIssue issue in issues.Where(IsAllowedLocation))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (exportedKeys.ContainsKey(issue.FullKeyPath))
                {
                    continue;
                }

                string file = Path.Combine(tempDirectory, $"{exportedKeys.Count:D4}.reg");
                bool exported = TryExportKey(issue.FullKeyPath, file);
                exportedKeys[issue.FullKeyPath] = exported;
                if (exported)
                {
                    exportedContents.Add(File.ReadAllText(file));
                }
                else
                {
                    logger.LogWarning("registry", $"Sauvegarde impossible, clé ignorée : {issue.FullKeyPath}");
                }
            }

            string? backupPath = null;
            if (exportedContents.Count > 0)
            {
                Directory.CreateDirectory(BackupDirectory);
                backupPath = Path.Combine(BackupDirectory, $"registre-{DateTime.Now:yyyyMMdd-HHmmss-fff}.reg");
                File.WriteAllText(backupPath, MergeRegExports(exportedContents), Encoding.Unicode);
                PruneOldBackups();
            }

            int removed = 0;
            int skipped = 0;
            foreach (RegistryIssue issue in issues)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool backedUp = exportedKeys.TryGetValue(issue.FullKeyPath, out bool ok) && ok;
                if (backedUp && TryApply(issue))
                {
                    removed++;
                }
                else
                {
                    skipped++;
                }
            }

            logger.LogInfo("registry", $"Nettoyage registre : {removed} supprimée(s), {skipped} ignorée(s), sauvegarde={backupPath ?? "aucune"}.");
            return new RegistryCleanResult
            {
                RemovedCount = removed,
                SkippedCount = skipped,
                BackupPath = backupPath
            };
        }
        finally
        {
            try { Directory.Delete(tempDirectory, recursive: true); } catch { }
        }
    }

    public string? GetLatestBackupPath()
    {
        try
        {
            if (!Directory.Exists(BackupDirectory))
            {
                return null;
            }

            return Directory.EnumerateFiles(BackupDirectory, "registre-*.reg")
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public bool TryRestoreBackup(string backupPath, out string message)
    {
        if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath))
        {
            message = "Sauvegarde introuvable.";
            return false;
        }

        try
        {
            (int exitCode, string output) = RunReg($"import \"{backupPath}\" /reg:64", TimeSpan.FromMinutes(1));
            if (exitCode == 0)
            {
                logger.LogInfo("registry", $"Registre restauré depuis {backupPath}.");
                message = $"Registre restauré depuis {Path.GetFileName(backupPath)}.";
                return true;
            }

            logger.LogWarning("registry", $"Restauration échouée ({exitCode}) : {output}");
            message = "Restauration incomplète : relancez Panosse en administrateur si la sauvegarde contient des clés système.";
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError("registry", "Restauration du registre impossible.", ex);
            message = $"Restauration impossible : {ex.Message}";
            return false;
        }
    }

    internal static string MergeRegExports(IEnumerable<string> exportContents)
    {
        var builder = new StringBuilder();
        builder.Append(RegHeader).Append("\r\n");
        foreach (string content in exportContents)
        {
            string body = content.TrimStart('\uFEFF');
            if (body.StartsWith(RegHeader, StringComparison.Ordinal))
            {
                body = body[RegHeader.Length..];
            }

            builder.Append("\r\n").Append(body.Trim('\r', '\n')).Append("\r\n");
        }

        return builder.ToString();
    }

    internal bool IsAllowedLocation(RegistryIssue issue)
    {
        foreach (string root in allowedRoots)
        {
            bool isRoot = issue.KeyPath.Equals(root, StringComparison.OrdinalIgnoreCase);
            bool isBelow = issue.KeyPath.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
            // Une clé entière ne peut être supprimée que strictement sous une racine autorisée.
            if (issue.Kind == RegistryIssueKind.Key ? isBelow : isRoot || isBelow)
            {
                return true;
            }
        }

        return false;
    }

    private void ScanPrivacy(List<RegistryIssue> issues)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        foreach ((string keyPath, string description) in PrivacyKeys)
        {
            using RegistryKey? key = baseKey.OpenSubKey(keyPath);
            if (key is not null && CountValuesRecursive(key) > 0)
            {
                issues.Add(new RegistryIssue
                {
                    Category = RegistryIssueCategories.Privacy,
                    Hive = RegistryHive.CurrentUser,
                    KeyPath = keyPath,
                    Kind = RegistryIssueKind.ClearValues,
                    Description = description,
                    RiskLevel = "Low"
                });
            }
        }
    }

    private static void ScanRunKey(
        List<RegistryIssue> issues,
        Dictionary<string, RegistryPathState> pathCache,
        RegistryHive hive,
        string runKeyPath,
        string startupApprovedPath,
        string risk)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? runKey = baseKey.OpenSubKey(runKeyPath);
        if (runKey is null)
        {
            return;
        }

        using RegistryKey? approvedKey = baseKey.OpenSubKey(startupApprovedPath);
        HashSet<string> approvedNames = new(approvedKey?.GetValueNames() ?? [], StringComparer.OrdinalIgnoreCase);

        foreach (string name in runKey.GetValueNames())
        {
            if (string.IsNullOrEmpty(name) ||
                runKey.GetValue(name) is not string command ||
                !RegistryPathInspector.TryGetExecutablePath(command, out string target) ||
                GetCachedState(pathCache, target) != RegistryPathState.Missing)
            {
                continue;
            }

            issues.Add(new RegistryIssue
            {
                Category = RegistryIssueCategories.Startup,
                Hive = hive,
                KeyPath = runKeyPath,
                ValueName = name,
                Kind = RegistryIssueKind.Value,
                Description = $"« {name} » lance un programme introuvable",
                TargetPath = target,
                RiskLevel = risk
            });

            if (approvedNames.Contains(name))
            {
                issues.Add(new RegistryIssue
                {
                    Category = RegistryIssueCategories.Startup,
                    Hive = hive,
                    KeyPath = startupApprovedPath,
                    ValueName = name,
                    Kind = RegistryIssueKind.Value,
                    Description = $"État de démarrage de « {name} » (programme introuvable)",
                    TargetPath = target,
                    RiskLevel = risk
                });
            }
        }
    }

    private static void ScanAppPaths(
        List<RegistryIssue> issues,
        Dictionary<string, RegistryPathState> pathCache,
        RegistryHive hive,
        string risk)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? appPaths = baseKey.OpenSubKey(AppPathsKey);
        if (appPaths is null)
        {
            return;
        }

        foreach (string subKeyName in appPaths.GetSubKeyNames())
        {
            using RegistryKey? subKey = appPaths.OpenSubKey(subKeyName);
            if (subKey?.GetValue(string.Empty) is not string command ||
                !RegistryPathInspector.TryGetExecutablePath(command, out string target) ||
                GetCachedState(pathCache, target) != RegistryPathState.Missing)
            {
                continue;
            }

            issues.Add(new RegistryIssue
            {
                Category = RegistryIssueCategories.AppPaths,
                Hive = hive,
                KeyPath = $"{AppPathsKey}\\{subKeyName}",
                Kind = RegistryIssueKind.Key,
                Description = $"« {subKeyName} » pointe vers un programme introuvable",
                TargetPath = target,
                RiskLevel = risk
            });
        }
    }

    private static void ScanMuiCache(List<RegistryIssue> issues, Dictionary<string, RegistryPathState> pathCache)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(MuiCacheKey);
        if (key is null)
        {
            return;
        }

        foreach (string name in key.GetValueNames())
        {
            if (!RegistryPathInspector.TryParseMuiCacheValueName(name, out string target) ||
                GetCachedState(pathCache, target) != RegistryPathState.Missing)
            {
                continue;
            }

            issues.Add(new RegistryIssue
            {
                Category = RegistryIssueCategories.MuiCache,
                Hive = RegistryHive.CurrentUser,
                KeyPath = MuiCacheKey,
                ValueName = name,
                Kind = RegistryIssueKind.Value,
                Description = $"Nom mis en cache pour {Path.GetFileName(target)} (supprimé)",
                TargetPath = target,
                RiskLevel = "Low"
            });
        }
    }

    private static void ScanPathNamedValues(
        List<RegistryIssue> issues,
        Dictionary<string, RegistryPathState> pathCache,
        RegistryHive hive,
        string keyPath,
        string category,
        string risk)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? key = baseKey.OpenSubKey(keyPath);
        if (key is null)
        {
            return;
        }

        foreach (string name in key.GetValueNames())
        {
            if (string.IsNullOrEmpty(name) || GetCachedState(pathCache, name) != RegistryPathState.Missing)
            {
                continue;
            }

            issues.Add(new RegistryIssue
            {
                Category = category,
                Hive = hive,
                KeyPath = keyPath,
                ValueName = name,
                Kind = RegistryIssueKind.Value,
                Description = $"{Path.GetFileName(name)} n'existe plus",
                TargetPath = name,
                RiskLevel = risk
            });
        }
    }

    private static void ScanUninstall(
        List<RegistryIssue> issues,
        Dictionary<string, RegistryPathState> pathCache,
        RegistryHive hive,
        string uninstallKeyPath)
    {
        using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using RegistryKey? uninstall = baseKey.OpenSubKey(uninstallKeyPath);
        if (uninstall is null)
        {
            return;
        }

        foreach (string subKeyName in uninstall.GetSubKeyNames())
        {
            using RegistryKey? entry = uninstall.OpenSubKey(subKeyName);
            if (entry?.GetValue("DisplayName") is not string displayName ||
                string.IsNullOrWhiteSpace(displayName) ||
                entry.GetValue("SystemComponent") is int and 1 ||
                entry.GetValue("WindowsInstaller") is int and 1 ||
                entry.GetValue("ParentKeyName") is not null ||
                entry.GetValue("UninstallString") is not string uninstallCommand ||
                !RegistryPathInspector.TryGetExecutablePath(uninstallCommand, out string uninstaller) ||
                GetCachedState(pathCache, uninstaller) != RegistryPathState.Missing)
            {
                continue;
            }

            if (entry.GetValue("InstallLocation") is string installLocation &&
                !string.IsNullOrWhiteSpace(installLocation) &&
                GetCachedState(pathCache, installLocation.Trim().Trim('"')) != RegistryPathState.Missing)
            {
                continue;
            }

            issues.Add(new RegistryIssue
            {
                Category = RegistryIssueCategories.Uninstall,
                Hive = hive,
                KeyPath = $"{uninstallKeyPath}\\{subKeyName}",
                Kind = RegistryIssueKind.Key,
                Description = $"« {displayName.Trim()} » n'est plus installé (désinstalleur introuvable)",
                TargetPath = uninstaller,
                RiskLevel = "Medium"
            });
        }
    }

    private bool TryApply(RegistryIssue issue)
    {
        if (!IsAllowedLocation(issue))
        {
            logger.LogWarning("registry", $"Emplacement non autorisé, ignoré : {issue.FullKeyPath}");
            return false;
        }

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(issue.Hive, RegistryView.Registry64);
            switch (issue.Kind)
            {
                case RegistryIssueKind.Value:
                {
                    using RegistryKey? key = baseKey.OpenSubKey(issue.KeyPath, writable: true);
                    if (key is null || string.IsNullOrEmpty(issue.ValueName))
                    {
                        return false;
                    }

                    key.DeleteValue(issue.ValueName, throwOnMissingValue: false);
                    return true;
                }
                case RegistryIssueKind.Key:
                {
                    int separator = issue.KeyPath.LastIndexOf('\\');
                    using RegistryKey? parent = baseKey.OpenSubKey(issue.KeyPath[..separator], writable: true);
                    if (parent is null)
                    {
                        return false;
                    }

                    parent.DeleteSubKeyTree(issue.KeyPath[(separator + 1)..], throwOnMissingSubKey: false);
                    return true;
                }
                case RegistryIssueKind.ClearValues:
                {
                    using RegistryKey? key = baseKey.OpenSubKey(issue.KeyPath, writable: true);
                    if (key is null)
                    {
                        return false;
                    }

                    ClearValuesRecursive(key);
                    return true;
                }
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("registry", $"Suppression impossible ({issue.FullKeyPath} / {issue.ValueName}) : {ex.Message}");
            return false;
        }
    }

    private static void ClearValuesRecursive(RegistryKey key)
    {
        foreach (string valueName in key.GetValueNames().Where(name => !string.IsNullOrEmpty(name)))
        {
            try { key.DeleteValue(valueName, throwOnMissingValue: false); } catch { }
        }

        foreach (string subKeyName in key.GetSubKeyNames())
        {
            try
            {
                using RegistryKey? subKey = key.OpenSubKey(subKeyName, writable: true);
                if (subKey is not null)
                {
                    ClearValuesRecursive(subKey);
                }
            }
            catch { }
        }
    }

    private static int CountValuesRecursive(RegistryKey key)
    {
        int count = key.GetValueNames().Count(name => !string.IsNullOrEmpty(name));
        foreach (string subKeyName in key.GetSubKeyNames())
        {
            using RegistryKey? subKey = key.OpenSubKey(subKeyName);
            if (subKey is not null)
            {
                count += CountValuesRecursive(subKey);
            }
        }

        return count;
    }

    private static RegistryPathState GetCachedState(Dictionary<string, RegistryPathState> cache, string path)
    {
        if (!cache.TryGetValue(path, out RegistryPathState state))
        {
            state = RegistryPathInspector.GetPathState(path);
            cache[path] = state;
        }

        return state;
    }

    private static bool IsIssueExcluded(RegistryIssue issue, IReadOnlyCollection<string> patterns) =>
        patterns.Count > 0 &&
        (CleanupOrchestrator.IsExcluded(issue.FullKeyPath, patterns) ||
         CleanupOrchestrator.IsExcluded(issue.ValueName ?? string.Empty, patterns) ||
         CleanupOrchestrator.IsExcluded(issue.TargetPath ?? string.Empty, patterns) ||
         CleanupOrchestrator.IsExcluded(issue.Description, patterns));

    private bool TryExportKey(string fullKeyPath, string destination)
    {
        try
        {
            (int exitCode, string output) = RunReg($"export \"{fullKeyPath}\" \"{destination}\" /y /reg:64", TimeSpan.FromSeconds(30));
            if (exitCode == 0 && File.Exists(destination))
            {
                return true;
            }

            logger.LogWarning("registry", $"reg export a échoué ({exitCode}) pour {fullKeyPath} : {output}");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning("registry", $"reg export impossible pour {fullKeyPath} : {ex.Message}");
            return false;
        }
    }

    private static (int ExitCode, string Output) RunReg(string arguments, TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "reg.exe"),
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("reg.exe n'a pas pu démarrer.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeout))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return (-1, "Délai dépassé.");
        }

        return (process.ExitCode, $"{stdout.Result}{stderr.Result}".Trim());
    }

    private void PruneOldBackups()
    {
        try
        {
            foreach (string stale in Directory.EnumerateFiles(BackupDirectory, "registre-*.reg")
                         .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                         .Skip(MaxBackups))
            {
                try { File.Delete(stale); } catch { }
            }
        }
        catch
        {
            // La rotation des sauvegardes ne doit jamais bloquer le nettoyage.
        }
    }

    private static string DefaultBackupDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Panosse",
            "RegistryBackups");

    private static bool IsProcessElevated()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
