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
