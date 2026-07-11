using System;
using System.IO;

namespace Panosse.Services;

public sealed class TelemetryService : ITelemetryService
{
    private readonly object syncRoot = new();
    private readonly string countersPath;

    public TelemetryService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "Panosse");
        countersPath = Path.Combine(appFolder, "telemetry_counters.json");
    }

    public void Increment(string counterName, long amount = 1)
    {
        AddToCounter(counterName, amount);
    }

    public void AddToCounter(string counterName, long amount)
    {
        if (string.IsNullOrWhiteSpace(counterName) || amount == 0)
        {
            return;
        }

        lock (syncRoot)
        {
            TelemetryCounters counters = LoadNoThrow();
            counters.Counters.TryGetValue(counterName, out long currentValue);
            counters.Counters[counterName] = currentValue + amount;
            counters.LastUpdatedUtc = DateTime.UtcNow;
            SaveNoThrow(counters);
        }
    }

    public void RecordDuration(string metricName, TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(metricName))
        {
            return;
        }

        long durationMs = Math.Max(0, (long)duration.TotalMilliseconds);
        lock (syncRoot)
        {
            TelemetryCounters counters = LoadNoThrow();
            string totalKey = $"{metricName}_total_ms";
            string countKey = $"{metricName}_count";

            counters.Counters.TryGetValue(totalKey, out long currentTotal);
            counters.Counters.TryGetValue(countKey, out long currentCount);

            counters.Counters[totalKey] = currentTotal + durationMs;
            counters.Counters[countKey] = currentCount + 1;
            counters.LastUpdatedUtc = DateTime.UtcNow;
            SaveNoThrow(counters);
        }
    }

    private TelemetryCounters LoadNoThrow()
    {
        try
        {
            if (!File.Exists(countersPath))
            {
                return new TelemetryCounters();
            }

            string json = File.ReadAllText(countersPath);
            TelemetryCounters? counters = System.Text.Json.JsonSerializer.Deserialize(
                json,
                PanosseJsonContext.Default.TelemetryCounters);
            return counters ?? new TelemetryCounters();
        }
        catch
        {
            return new TelemetryCounters();
        }
    }

    private void SaveNoThrow(TelemetryCounters counters)
    {
        try
        {
            string? directory = Path.GetDirectoryName(countersPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = System.Text.Json.JsonSerializer.Serialize(
                counters,
                PanosseJsonContext.Default.TelemetryCounters);
            File.WriteAllText(countersPath, json);
        }
        catch
        {
            // Ne jamais impacter l'exécution métier.
        }
    }
}
