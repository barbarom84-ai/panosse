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
