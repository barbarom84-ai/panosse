using System;

namespace Panosse.Services;

public sealed class OperationHistoryEntry
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string OperationType { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public long FreedBytes { get; set; }
    public long DurationMs { get; set; }
    public string Details { get; set; } = string.Empty;
}
