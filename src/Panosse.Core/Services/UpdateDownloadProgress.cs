namespace Panosse.Services;

public sealed class UpdateDownloadProgress
{
    public int ProgressPercent { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LocalFilePath { get; set; }
}
