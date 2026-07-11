using System.Collections.Generic;

namespace Panosse.Services;

public sealed class CleanupExecutionOptions
{
    public bool PreviewOnly { get; set; }
    public List<string> ExclusionPatterns { get; set; } = new();
}
