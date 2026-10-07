using Panosse.Services;
using Xunit;

namespace Panosse.Core.Tests;

public class CleanupPathsTests
{
    [Fact]
    public void EstimateDirectoryBytes_RespectsAgeThreshold()
    {
        string root = Path.Combine(Path.GetTempPath(), "panosse-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        try
        {
            string oldFile = Path.Combine(root, "sub", "old.cab");
            string recentFile = Path.Combine(root, "recent.cab");
            File.WriteAllBytes(oldFile, new byte[300]);
            File.WriteAllBytes(recentFile, new byte[200]);
            File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-30));

            Assert.Equal(500, CleanupPaths.EstimateDirectoryBytes(root));
            Assert.Equal(300, CleanupPaths.EstimateDirectoryBytes(root, olderThanDays: 7));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void EstimateDirectoryBytes_MissingDirectory_ReturnsZero()
    {
        Assert.Equal(0, CleanupPaths.EstimateDirectoryBytes(@"Z:\panosse\does-not-exist"));
        Assert.Equal(0, CleanupPaths.GetFileLength(@"Z:\panosse\missing.sys"));
    }

    [Fact]
    public void DeveloperCaches_TargetOnlyPackageCaches()
    {
        IReadOnlyList<string> paths = CleanupPaths.DeveloperCacheDirectories();

        Assert.Contains(paths, p => p.EndsWith("npm-cache", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(paths, p => p.EndsWith(@"pip\Cache", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(paths, p => p.EndsWith("caches", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, p => p.Contains("huggingface", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(paths, p => p.Contains(".ollama", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(512L * 1024 * 1024, "512 Mo")]
    [InlineData(27_387_920_384L, "25,5 Go")]
    public void StorageReportItem_FormatsSize(long bytes, string expected)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            Assert.Equal(expected, StorageReportItem.FormatSize(bytes));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }
}
