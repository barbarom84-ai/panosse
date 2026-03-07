using System;
using System.Collections.Generic;

namespace Panosse.Services;

public sealed class TelemetryCounters
{
    public Dictionary<string, long> Counters { get; set; } = new();
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
