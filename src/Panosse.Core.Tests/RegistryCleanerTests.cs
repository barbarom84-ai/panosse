using Microsoft.Win32;
using Panosse.Services;
using Xunit;

namespace Panosse.Core.Tests;

internal sealed class NullLogger : ILoggerService
{
    public void LogInfo(string category, string message) { }
    public void LogWarning(string category, string message) { }
    public void LogError(string category, string message, Exception? exception = null) { }
}

public class RegistryPathInspectorTests
{
    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\" --minimized", "C:\\Program Files\\App\\app.exe")]
    [InlineData("C:\\Program Files\\App\\app.exe --minimized", "C:\\Program Files\\App\\app.exe")]
    [InlineData("C:\\Tools\\tool.exe", "C:\\Tools\\tool.exe")]
    [InlineData("C:\\Program Files\\App\\uninst.exe /S", "C:\\Program Files\\App\\uninst.exe")]
    public void TryGetExecutablePath_ParsesCommandLines(string commandLine, string expected)
    {
        Assert.True(RegistryPathInspector.TryGetExecutablePath(commandLine, out string path));
        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ctfmon.exe")]
    [InlineData("MsiExec.exe /X{12345678-1234-1234-1234-123456789012}")]
    [InlineData("\\\\server\\share\\app.exe")]
    [InlineData("%PANOSSE_UNDEFINED_VARIABLE%\\app.exe")]
    [InlineData("C:\\Program Files\\App\\app --flag")]
    [InlineData("\"C:\\unterminated\\app.exe")]
    public void TryGetExecutablePath_RejectsAmbiguousOrNonLocal(string? commandLine)
    {
        Assert.False(RegistryPathInspector.TryGetExecutablePath(commandLine, out _));
    }

    [Fact]
    public void TryGetExecutablePath_ExpandsEnvironmentVariables()
    {
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        Assert.True(RegistryPathInspector.TryGetExecutablePath("\"%ProgramFiles%\\App\\app.exe\"", out string path));
        Assert.Equal(Path.Combine(programFiles, "App", "app.exe"), path);
    }

    [Fact]
    public void GetPathState_DistinguishesExistingAndMissingFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"panosse-path-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string existing = Path.Combine(directory, "present.exe");
        File.WriteAllText(existing, "x");
        try
        {
            Assert.Equal(RegistryPathState.Exists, RegistryPathInspector.GetPathState(existing));
            Assert.Equal(RegistryPathState.Missing, RegistryPathInspector.GetPathState(Path.Combine(directory, "absent.exe")));
            Assert.Equal(RegistryPathState.Missing, RegistryPathInspector.GetPathState(Path.Combine(directory, "gone", "deeper", "absent.exe")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void GetPathState_NeverReportsWindowsOrNetworkPathsAsMissing()
    {
        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        Assert.Equal(RegistryPathState.Unknown, RegistryPathInspector.GetPathState(Path.Combine(windows, "System32", "panosse-absent.exe")));
        Assert.Equal(RegistryPathState.Unknown, RegistryPathInspector.GetPathState(@"\\server\share\absent.exe"));
        Assert.Equal(RegistryPathState.Unknown, RegistryPathInspector.GetPathState("relative\\absent.exe"));
    }

    [Theory]
    [InlineData("C:\\Apps\\tool.exe.FriendlyAppName", "C:\\Apps\\tool.exe")]
    [InlineData("C:\\Apps\\tool.exe.ApplicationCompany", "C:\\Apps\\tool.exe")]
    public void TryParseMuiCacheValueName_ExtractsPath(string valueName, string expected)
    {
        Assert.True(RegistryPathInspector.TryParseMuiCacheValueName(valueName, out string path));
        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData("@C:\\Windows\\system32\\shell32.dll,-1")]
    [InlineData("LangID")]
    [InlineData("tool.exe.FriendlyAppName")]
    public void TryParseMuiCacheValueName_RejectsOtherEntries(string valueName)
    {
        Assert.False(RegistryPathInspector.TryParseMuiCacheValueName(valueName, out _));
    }
}

public class RegistryCleanerServiceTests
{
    private const string TestRoot = @"Software\PanosseTests";

    [Fact]
    public void MergeRegExports_KeepsSingleHeaderAndAllBodies()
    {
        string first = "\uFEFFWindows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\A]\r\n\"x\"=\"1\"\r\n\r\n";
        string second = "Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\B]\r\n\"y\"=dword:00000002\r\n";

        string merged = RegistryCleanerService.MergeRegExports([first, second]);

        Assert.StartsWith("Windows Registry Editor Version 5.00\r\n", merged);
        Assert.Equal(1, merged.Split("Windows Registry Editor").Length - 1);
        Assert.Contains("[HKEY_CURRENT_USER\\A]\r\n\"x\"=\"1\"", merged);
        Assert.Contains("[HKEY_CURRENT_USER\\B]\r\n\"y\"=dword:00000002", merged);
    }

    [Fact]
    public void IsAllowedLocation_RefusesWholeRootKeysAndForeignPaths()
    {
        var service = new RegistryCleanerService(new NullLogger(), Path.GetTempPath(), () => false);
        const string run = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string uninstall = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        Assert.True(service.IsAllowedLocation(new RegistryIssue { KeyPath = run, ValueName = "x", Kind = RegistryIssueKind.Value }));
        Assert.True(service.IsAllowedLocation(new RegistryIssue { KeyPath = uninstall + @"\App", Kind = RegistryIssueKind.Key }));
        Assert.False(service.IsAllowedLocation(new RegistryIssue { KeyPath = uninstall, Kind = RegistryIssueKind.Key }));
        Assert.False(service.IsAllowedLocation(new RegistryIssue { KeyPath = @"Software\Microsoft\Windows", Kind = RegistryIssueKind.Key }));
        Assert.False(service.IsAllowedLocation(new RegistryIssue { KeyPath = @"Software\Microsoft\Windows\CurrentVersion\RunOnce", ValueName = "x", Kind = RegistryIssueKind.Value }));
    }

    [Fact]
    public void Scan_RapidProfile_DoesNothing()
    {
        var service = new RegistryCleanerService(new NullLogger(), Path.GetTempPath(), () => false);
        Assert.Empty(service.Scan(CleanupProfiles.Rapid));
    }

    [Fact]
    public void Scan_StandardProfile_OnlyReturnsLowRiskUserEntries()
    {
        var service = new RegistryCleanerService(new NullLogger(), Path.GetTempPath(), () => true);
        IReadOnlyList<RegistryIssue> issues = service.Scan(CleanupProfiles.Standard);

        Assert.All(issues, issue => Assert.Equal("Low", issue.RiskLevel));
        Assert.All(issues, issue => Assert.Equal(RegistryHive.CurrentUser, issue.Hive));
        Assert.All(issues, issue => Assert.True(service.IsAllowedLocation(issue)));
    }

    [Fact]
    public void Scan_DeepProfile_ProducesOnlyAllowedLocations()
    {
        var service = new RegistryCleanerService(new NullLogger(), Path.GetTempPath(), () => true);
        IReadOnlyList<RegistryIssue> issues = service.Scan(CleanupProfiles.Deep);

        Assert.All(issues, issue => Assert.True(service.IsAllowedLocation(issue)));
        Assert.All(issues.Where(i => i.Category != RegistryIssueCategories.Privacy), issue => Assert.False(string.IsNullOrEmpty(issue.TargetPath)));
    }

    [Fact]
    public void Clean_BacksUpDeletesAndRestores()
    {
        string testKey = $@"{TestRoot}\{Guid.NewGuid():N}";
        string backupDirectory = Path.Combine(Path.GetTempPath(), $"panosse-regbackup-{Guid.NewGuid():N}");
        var service = new RegistryCleanerService(new NullLogger(), backupDirectory, () => false, [TestRoot]);

        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(testKey))
            {
                key.SetValue("Doomed", "C:\\Missing\\app.exe");
                key.SetValue("Kept", 42, RegistryValueKind.DWord);
                using RegistryKey child = key.CreateSubKey("Orphan");
                child.SetValue("DisplayName", "Logiciel fantôme");
            }

            RegistryIssue[] issues =
            [
                new() { Hive = RegistryHive.CurrentUser, KeyPath = testKey, ValueName = "Doomed", Kind = RegistryIssueKind.Value },
                new() { Hive = RegistryHive.CurrentUser, KeyPath = testKey + @"\Orphan", Kind = RegistryIssueKind.Key }
            ];

            RegistryCleanResult result = service.Clean(issues);

            Assert.Equal(2, result.RemovedCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.NotNull(result.BackupPath);
            Assert.True(File.Exists(result.BackupPath));
            Assert.Contains("Orphan", File.ReadAllText(result.BackupPath!));
            Assert.Equal(result.BackupPath, service.GetLatestBackupPath());

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(testKey))
            {
                Assert.NotNull(key);
                Assert.Null(key!.GetValue("Doomed"));
                Assert.Equal(42, key.GetValue("Kept"));
                Assert.Null(key.OpenSubKey("Orphan"));
            }

            Assert.True(service.TryRestoreBackup(result.BackupPath!, out string message), message);

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(testKey))
            {
                Assert.Equal("C:\\Missing\\app.exe", key!.GetValue("Doomed"));
                using RegistryKey? child = key.OpenSubKey("Orphan");
                Assert.Equal("Logiciel fantôme", child?.GetValue("DisplayName"));
            }
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(testKey, throwOnMissingSubKey: false);
            using (RegistryKey? root = Registry.CurrentUser.OpenSubKey(TestRoot, writable: true))
            {
                if (root is not null && root.SubKeyCount == 0 && root.ValueCount == 0)
                {
                    Registry.CurrentUser.DeleteSubKey(TestRoot, throwOnMissingSubKey: false);
                }
            }

            if (Directory.Exists(backupDirectory))
            {
                Directory.Delete(backupDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Clean_SkipsEntriesOutsideAllowedRoots()
    {
        string backupDirectory = Path.Combine(Path.GetTempPath(), $"panosse-regbackup-{Guid.NewGuid():N}");
        var service = new RegistryCleanerService(new NullLogger(), backupDirectory, () => false);

        RegistryCleanResult result = service.Clean(
        [
            new RegistryIssue { Hive = RegistryHive.CurrentUser, KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", Kind = RegistryIssueKind.Key }
        ]);

        Assert.Equal(0, result.RemovedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Null(result.BackupPath);
        Assert.False(Directory.Exists(backupDirectory));
    }

    [Fact]
    public void FormatRegistryStepMessage_ReportsCountsAndBackup()
    {
        Assert.Contains("aucune", CleanupOrchestrator.FormatRegistryStepMessage(true, 0, null));
        Assert.Contains("7 entrée(s)", CleanupOrchestrator.FormatRegistryStepMessage(true, 7, null));

        string message = CleanupOrchestrator.FormatRegistryStepMessage(
            false,
            5,
            new RegistryCleanResult { RemovedCount = 4, SkippedCount = 1, BackupPath = @"C:\b\registre-20261006.reg" });

        Assert.Contains("4 entrée(s) nettoyée(s)", message);
        Assert.Contains("1 ignorée(s)", message);
        Assert.Contains("registre-20261006.reg", message);
    }

    [Fact]
    public void FormatPreviewLine_ShowsEntryCountForRegistry()
    {
        string line = Panosse.Core.ViewModels.ShellViewModel.FormatPreviewLine(
            new CleanupPreviewItem { Category = "Registre · Démarrage invalide", ItemCount = 3, Location = "« Foo » lance un programme introuvable" },
            "faible");

        Assert.Contains("3 entrée(s)", line);
        Assert.Contains("Foo", line);
        Assert.DoesNotContain("Mo", line);
    }
}
