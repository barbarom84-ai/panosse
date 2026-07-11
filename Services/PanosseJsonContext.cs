using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Panosse.Services;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(TelemetryCounters))]
[JsonSerializable(typeof(List<OperationHistoryEntry>))]
internal sealed partial class PanosseJsonContext : JsonSerializerContext
{
}
