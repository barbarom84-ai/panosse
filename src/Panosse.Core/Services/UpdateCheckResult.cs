namespace Panosse.Services;

public sealed class UpdateReleaseInfo
{
    public string TagName { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string ExeFileName { get; init; } = string.Empty;
    public string? ExpectedSha256 { get; init; }
}

public sealed class UpdateCheckResult
{
    public bool IsUpToDate { get; init; }
    public bool HasUpdate { get; init; }
    public bool VerificationFailed { get; init; }
    public UpdateReleaseInfo? ReleaseInfo { get; init; }

    public static UpdateCheckResult Failed() => new() { VerificationFailed = true };
    public static UpdateCheckResult UpToDate() => new() { IsUpToDate = true };
    public static UpdateCheckResult UpdateAvailable(UpdateReleaseInfo releaseInfo) =>
        new() { HasUpdate = true, ReleaseInfo = releaseInfo };
}
