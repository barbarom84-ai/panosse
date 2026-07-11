using System.Collections.Generic;

namespace Panosse.Services;

public sealed class CleanupRunResult
{
    public bool IsPreview { get; set; }
    public long TotalFreedBytes { get; set; }
    public List<CleanupPreviewItem> PreviewItems { get; set; } = new();
}
