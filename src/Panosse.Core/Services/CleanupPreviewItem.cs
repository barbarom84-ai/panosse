namespace Panosse.Services;

public sealed class CleanupPreviewItem
{
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public long EstimatedBytes { get; set; }
    public int ItemCount { get; set; }
    public string RiskLevel { get; set; } = "Low";
}
