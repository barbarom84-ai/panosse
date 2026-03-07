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
                Timeout = TimeSpan.FromSeconds(10)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Panosse-App/1.0");

            string apiUrl = $"https://api.github.com/repos/{githubRepo}/releases/latest";
            string responseContent = await client.GetStringAsync(apiUrl, cancellationToken);

            using JsonDocument doc = JsonDocument.Parse(responseContent);
            JsonElement root = doc.RootElement;

            // Null/absence checks for all API fields before parsing.
            string? tagName = TryGetStringProperty(root, "tag_name");
            string? htmlUrl = TryGetStringProperty(root, "html_url");

            if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(htmlUrl))
            {
                return UpdateCheckResult.Failed();
            }

            string downloadUrl = TryGetExeDownloadUrl(root) ?? string.Empty;
            string remoteVersion = tagName.TrimStart('v');

            if (!IsRemoteVersionNewer(remoteVersion, currentVersion))
            {
                return UpdateCheckResult.UpToDate();
            }

            return UpdateCheckResult.UpdateAvailable(new UpdateReleaseInfo
            {
                TagName = tagName,
                HtmlUrl = htmlUrl,
                DownloadUrl = downloadUrl
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

    private static string? TryGetExeDownloadUrl(JsonElement root)
    {
        if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement asset in assets.EnumerateArray())
        {
            string? assetName = TryGetStringProperty(asset, "name");
            if (string.IsNullOrWhiteSpace(assetName) ||
                !assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? browserUrl = TryGetStringProperty(asset, "browser_download_url");
            if (!string.IsNullOrWhiteSpace(browserUrl))
            {
                return browserUrl;
            }
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
        try
        {
            remoteVersion = remoteVersion.Split('-')[0];
            localVersion = localVersion.Split('-')[0];

            var remoteParts = remoteVersion.Split('.').Select(int.Parse).ToArray();
            var localParts = localVersion.Split('.').Select(int.Parse).ToArray();

            if (remoteParts[0] > localParts[0]) return true;
            if (remoteParts[0] < localParts[0]) return false;

            if (remoteParts.Length > 1 && localParts.Length > 1)
            {
                if (remoteParts[1] > localParts[1]) return true;
                if (remoteParts[1] < localParts[1]) return false;
            }

            if (remoteParts.Length > 2 && localParts.Length > 2)
            {
                if (remoteParts[2] > localParts[2]) return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
