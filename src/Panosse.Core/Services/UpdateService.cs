using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Panosse.Services;

public sealed class UpdateService : IUpdateService
{
    public async Task<UpdateCheckResult> CheckForUpdateAsync(
        string githubRepo,
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Panosse-App/1.0");

            string apiUrl = $"https://api.github.com/repos/{githubRepo}/releases/latest";
            string responseContent = await client.GetStringAsync(apiUrl, cancellationToken);

            using JsonDocument doc = JsonDocument.Parse(responseContent);
            JsonElement root = doc.RootElement;

            string? tagName = TryGetStringProperty(root, "tag_name");
            string? htmlUrl = TryGetStringProperty(root, "html_url");

            if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(htmlUrl))
            {
                return UpdateCheckResult.Failed();
            }

            if (!TryGetPortableExeAsset(root, out string downloadUrl, out string exeFileName))
            {
                return UpdateCheckResult.Failed();
            }

            string remoteVersion = tagName.TrimStart('v');
            if (!IsRemoteVersionNewer(remoteVersion, currentVersion))
            {
                return UpdateCheckResult.UpToDate();
            }

            string? expectedSha256 = await TryGetExpectedSha256Async(client, root, exeFileName, cancellationToken);

            return UpdateCheckResult.UpdateAvailable(new UpdateReleaseInfo
            {
                TagName = tagName,
                HtmlUrl = htmlUrl,
                DownloadUrl = downloadUrl,
                ExeFileName = exeFileName,
                ExpectedSha256 = expectedSha256
            });
        }
        catch (HttpRequestException)
        {
            return UpdateCheckResult.Failed();
        }
        catch (TaskCanceledException)
        {
            return UpdateCheckResult.Failed();
        }
        catch (JsonException)
        {
            return UpdateCheckResult.Failed();
        }
        catch
        {
            return UpdateCheckResult.Failed();
        }
    }

    private static bool TryGetPortableExeAsset(JsonElement root, out string downloadUrl, out string exeFileName)
    {
        downloadUrl = string.Empty;
        exeFileName = string.Empty;

        if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        string? preferredUrl = null;
        string? preferredName = null;
        string? fallbackUrl = null;
        string? fallbackName = null;

        foreach (JsonElement asset in assets.EnumerateArray())
        {
            string? assetName = TryGetStringProperty(asset, "name");
            if (string.IsNullOrWhiteSpace(assetName) ||
                !assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                assetName.Contains("Setup", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? browserUrl = TryGetStringProperty(asset, "browser_download_url");
            if (string.IsNullOrWhiteSpace(browserUrl))
            {
                continue;
            }

            if (assetName.StartsWith("Panosse-v", StringComparison.OrdinalIgnoreCase))
            {
                preferredUrl = browserUrl;
                preferredName = assetName;
                break;
            }

            fallbackUrl ??= browserUrl;
            fallbackName ??= assetName;
        }

        if (preferredUrl is null && fallbackUrl is null)
        {
            return false;
        }

        downloadUrl = preferredUrl ?? fallbackUrl!;
        exeFileName = preferredName ?? fallbackName!;
        return true;
    }

    private static async Task<string?> TryGetExpectedSha256Async(
        HttpClient client,
        JsonElement root,
        string exeFileName,
        CancellationToken cancellationToken)
    {
        string? sumsUrl = TryGetAssetDownloadUrl(root, "SHA256SUMS.txt");
        if (string.IsNullOrWhiteSpace(sumsUrl))
        {
            return null;
        }

        string content = await client.GetStringAsync(sumsUrl, cancellationToken);
        foreach (string rawLine in content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();
            if (!line.EndsWith(exeFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] parts = line.Split([' '], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[1].Trim().Equals(exeFileName, StringComparison.OrdinalIgnoreCase))
            {
                return parts[0].Trim();
            }
        }

        return null;
    }

    private static string? TryGetAssetDownloadUrl(JsonElement root, string assetName)
    {
        if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement asset in assets.EnumerateArray())
        {
            string? name = TryGetStringProperty(asset, "name");
            if (!string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return TryGetStringProperty(asset, "browser_download_url");
        }

        return null;
    }

    private static string? TryGetStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static bool IsRemoteVersionNewer(string remoteVersion, string localVersion)
    {
        static Version NormalizeVersion(string input)
        {
            string cleaned = input.Split('-')[0].Trim();
            if (Version.TryParse(cleaned, out Version? parsed))
            {
                return parsed;
            }

            string[] tokens = cleaned.Split('.');
            while (tokens.Length < 3)
            {
                cleaned += ".0";
                tokens = cleaned.Split('.');
            }

            return Version.TryParse(cleaned, out parsed) ? parsed : new Version(0, 0, 0);
        }

        try
        {
            Version remote = NormalizeVersion(remoteVersion);
            Version local = NormalizeVersion(localVersion);
            return remote > local;
        }
        catch
        {
            return false;
        }
    }
}
