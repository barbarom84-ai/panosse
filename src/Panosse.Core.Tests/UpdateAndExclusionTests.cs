using Panosse.Services;
using Xunit;

namespace Panosse.Core.Tests;

public class UpdateVersionTests
{
    [Theory]
    [InlineData("v2.2.6", "2.2.6")]
    [InlineData("2.2.6.0", "2.2.6")]
    [InlineData("latest", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normalize_ReturnsExpected(string? input, string? expected)
    {
        Assert.Equal(expected, UpdateVersion.Normalize(input));
    }
}

public class ExclusionMatchingTests
{
    [Fact]
    public void IsExcluded_MatchesSubstringIgnoreCase()
    {
        Assert.True(CleanupOrchestrator.IsExcluded(@"C:\Downloads\KeepMe.exe", new[] { "keepme" }));
        Assert.False(CleanupOrchestrator.IsExcluded(@"C:\Downloads\Other.exe", new[] { "keepme" }));
        Assert.False(CleanupOrchestrator.IsExcluded(@"C:\Downloads\Other.exe", Array.Empty<string>()));
    }
}

public class HistoryFormattingTests
{
    [Fact]
    public void FormatHistoryLine_IncludesFrenchOutcomeAndFailureDetails()
    {
        var entry = new OperationHistoryEntry
        {
            TimestampUtc = new DateTime(2026, 7, 31, 10, 15, 0, DateTimeKind.Utc),
            OperationType = "cleanup_manual",
            Outcome = "failure",
            FreedBytes = 5 * 1024 * 1024,
            Details = "access denied"
        };

        string line = Panosse.Core.ViewModels.ShellViewModel.FormatHistoryLine(entry);

        Assert.Contains("Nettoyage", line);
        Assert.Contains("Échec", line);
        Assert.Contains("access denied", line);
        Assert.Contains("5", line);
    }
}

public class CleanupProfileTests
{
    [Theory]
    [InlineData("rapid", CleanupProfiles.Rapid)]
    [InlineData("Rapide", CleanupProfiles.Rapid)]
    [InlineData("deep", CleanupProfiles.Deep)]
    [InlineData("Profond", CleanupProfiles.Deep)]
    [InlineData(null, CleanupProfiles.Standard)]
    [InlineData("", CleanupProfiles.Standard)]
    public void Normalize_MapsAliases(string? input, string expected)
    {
        Assert.Equal(expected, CleanupProfiles.Normalize(input));
    }

    [Fact]
    public void Rapid_ExcludesBrowserDownloadsRegistryAndLogs()
    {
        Assert.True(CleanupProfiles.IncludesCategory(CleanupProfiles.Rapid, CleanupProfiles.CategoryTemp));
        Assert.True(CleanupProfiles.IncludesCategory(CleanupProfiles.Rapid, CleanupProfiles.CategoryRecycle));
        Assert.True(CleanupProfiles.IncludesCategory(CleanupProfiles.Rapid, CleanupProfiles.CategoryThumbnails));
        Assert.False(CleanupProfiles.IncludesCategory(CleanupProfiles.Rapid, CleanupProfiles.CategoryBrowser));
        Assert.False(CleanupProfiles.IncludesCategory(CleanupProfiles.Rapid, CleanupProfiles.CategoryDownloads));
        Assert.False(CleanupProfiles.IncludesCategory(CleanupProfiles.Rapid, CleanupProfiles.CategoryRegistry));
        Assert.False(CleanupProfiles.IncludesCategory(CleanupProfiles.Rapid, CleanupProfiles.CategoryLogs));
    }

    [Fact]
    public void Deep_RaisesRiskOnSensitiveCategories()
    {
        Assert.Equal("High", CleanupProfiles.GetRiskLevel(CleanupProfiles.Deep, CleanupProfiles.CategoryRegistry));
        Assert.Equal("High", CleanupProfiles.GetRiskLevel(CleanupProfiles.Deep, CleanupProfiles.CategoryDownloads));
        Assert.Equal("High", CleanupProfiles.GetRiskLevel(CleanupProfiles.Deep, CleanupProfiles.CategoryLogs));
        Assert.Equal("Medium", CleanupProfiles.GetRiskLevel(CleanupProfiles.Standard, CleanupProfiles.CategoryDownloads));
        Assert.Equal("Low", CleanupProfiles.GetRiskLevel(CleanupProfiles.Standard, CleanupProfiles.CategoryLogs));
    }

    [Fact]
    public void IndexRoundTrip_IsStable()
    {
        Assert.Equal(0, CleanupProfiles.ToIndex(CleanupProfiles.Rapid));
        Assert.Equal(1, CleanupProfiles.ToIndex(CleanupProfiles.Standard));
        Assert.Equal(2, CleanupProfiles.ToIndex(CleanupProfiles.Deep));
        Assert.Equal(CleanupProfiles.Rapid, CleanupProfiles.FromIndex(0));
        Assert.Equal(CleanupProfiles.Standard, CleanupProfiles.FromIndex(1));
        Assert.Equal(CleanupProfiles.Deep, CleanupProfiles.FromIndex(2));
    }
}

public class SettingsMigrationTests
{
    [Fact]
    public void MigrateIfNeeded_UpgradesSchemaZeroAndNormalizes()
    {
        var settings = new AppSettings
        {
            SchemaVersion = 0,
            ScheduledCleanupIntervalHours = 0,
            CleanupProfile = "profond"
        };

        bool migrated = SettingsService.MigrateIfNeeded(settings);

        Assert.True(migrated);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Equal(24, settings.ScheduledCleanupIntervalHours);
        Assert.Equal(CleanupProfiles.Deep, settings.CleanupProfile);
        Assert.Equal(100, settings.UiScalePercent);
    }

    [Fact]
    public void MigrateIfNeeded_CurrentSchema_ReturnsFalse()
    {
        var settings = new AppSettings
        {
            SchemaVersion = AppSettings.CurrentSchemaVersion,
            CleanupProfile = CleanupProfiles.Standard,
            ScheduledCleanupIntervalHours = 12,
            UiScalePercent = 110
        };

        bool migrated = SettingsService.MigrateIfNeeded(settings);

        Assert.False(migrated);
        Assert.Equal(12, settings.ScheduledCleanupIntervalHours);
        Assert.Equal(110, settings.UiScalePercent);
    }

    [Fact]
    public void MigrateIfNeeded_V1ToV2_AddsUiScale()
    {
        var settings = new AppSettings
        {
            SchemaVersion = 1,
            UiScalePercent = 0
        };

        bool migrated = SettingsService.MigrateIfNeeded(settings);

        Assert.True(migrated);
        Assert.Equal(2, settings.SchemaVersion);
        Assert.Equal(100, settings.UiScalePercent);
    }
}

public class UpdateShaTests
{
    [Fact]
    public void ValidateDownloadedExecutable_RejectsBadHash()
    {
        string path = Path.Combine(Path.GetTempPath(), $"panosse-sha-{Guid.NewGuid():N}.bin");
        File.WriteAllText(path, "hello");
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                UpdateDownloadService.ValidateDownloadedExecutable(path, "00"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ValidateDownloadedExecutable_AcceptsMatchingHash()
    {
        string path = Path.Combine(Path.GetTempPath(), $"panosse-sha-{Guid.NewGuid():N}.bin");
        File.WriteAllText(path, "hello");
        try
        {
            string hash = UpdateDownloadService.ComputeSha256Hex(path);
            UpdateDownloadService.ValidateDownloadedExecutable(path, hash);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}

public class DiagnosticsServiceTests
{
    [Fact]
    public void RunChecks_ReturnsAtLeastAdminAndTemp()
    {
        var service = new DiagnosticsService();
        IReadOnlyList<DiagnosticItem> items = service.RunChecks();

        Assert.Contains(items, i => i.Name.Contains("administrateur", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(items, i => i.Name.Contains("Temp", StringComparison.OrdinalIgnoreCase));
        Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.DisplayLine)));
    }
}
