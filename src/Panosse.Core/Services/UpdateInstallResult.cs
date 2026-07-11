namespace Panosse.Services;

public sealed class UpdateInstallResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string DownloadedExePath { get; set; } = string.Empty;
    public string ScriptPath { get; set; } = string.Empty;
}
