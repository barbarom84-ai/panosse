using System;
using System.IO;
using System.Text.Json;

namespace Panosse.Services;

public sealed class TelemetryService : ITelemetryService
{
    private readonly object syncRoot = new();
    private readonly string countersPath;
    private readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };

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

    private TelemetryCounters LoadNoThrow()
    {
        try
        {
            if (!File.Exists(countersPath))
            {
                return new TelemetryCounters();
            }

            string json = File.ReadAllText(countersPath);
            TelemetryCounters? counters = JsonSerializer.Deserialize<TelemetryCounters>(json);
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

            string json = JsonSerializer.Serialize(counters, serializerOptions);
            File.WriteAllText(countersPath, json);
        }
        catch
        {
            // Ne jamais impacter l'exécution métier.
        }
    }
}
