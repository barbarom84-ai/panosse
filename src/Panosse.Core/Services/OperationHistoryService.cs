using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Panosse.Services;

public sealed class OperationHistoryService : IOperationHistoryService
{
    private const int MaxStoredEntries = 200;
    private readonly object syncRoot = new();
    private readonly string historyPath;

    public OperationHistoryService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "Panosse");
        historyPath = Path.Combine(appFolder, "history.json");
    }

    public void AddEntry(OperationHistoryEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        lock (syncRoot)
        {
            List<OperationHistoryEntry> entries = LoadNoThrow();
            entries.Add(entry);
            if (entries.Count > MaxStoredEntries)
            {
                entries = entries
                    .OrderByDescending(e => e.TimestampUtc)
                    .Take(MaxStoredEntries)
                    .OrderBy(e => e.TimestampUtc)
                    .ToList();
            }

            SaveNoThrow(entries);
        }
    }

    public IReadOnlyList<OperationHistoryEntry> GetRecentEntries(int maxEntries = 20)
    {
        if (maxEntries <= 0)
        {
            return Array.Empty<OperationHistoryEntry>();
        }

        lock (syncRoot)
        {
            return LoadNoThrow()
                .OrderByDescending(e => e.TimestampUtc)
                .Take(maxEntries)
                .ToList();
        }
    }

    public void Clear()
    {
        lock (syncRoot)
        {
            SaveNoThrow(new List<OperationHistoryEntry>());
        }
    }

    public string ExportToCsv(int maxEntries = 100)
    {
        List<OperationHistoryEntry> entries;
        lock (syncRoot)
        {
            entries = LoadNoThrow()
                .OrderByDescending(e => e.TimestampUtc)
                .Take(Math.Max(1, maxEntries))
                .ToList();
        }

        string? directory = Path.GetDirectoryName(historyPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("Impossible de déterminer le dossier d'historique.");
        }

        Directory.CreateDirectory(directory);
        string exportPath = Path.Combine(directory, $"history-export-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        var lines = new List<string>
        {
            "TimestampLocal,OperationType,Outcome,FreedMb,DurationMs,Details"
        };

        foreach (OperationHistoryEntry entry in entries)
        {
            double mb = Math.Round(entry.FreedBytes / 1024.0 / 1024.0, 2);
            lines.Add(string.Join(',',
                Csv(entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(entry.OperationType),
                Csv(entry.Outcome),
                Csv(mb.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                Csv(entry.DurationMs.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                Csv(entry.Details)));
        }

        File.WriteAllLines(exportPath, lines);
        return exportPath;
    }

    private static string Csv(string? value)
    {
        string raw = value ?? string.Empty;
        if (raw.Contains('"') || raw.Contains(',') || raw.Contains('\n') || raw.Contains('\r'))
        {
            return $"\"{raw.Replace("\"", "\"\"")}\"";
        }

        return raw;
    }

    private List<OperationHistoryEntry> LoadNoThrow()
    {
        try
        {
            if (!File.Exists(historyPath))
            {
                return new List<OperationHistoryEntry>();
            }

            string json = File.ReadAllText(historyPath);
            List<OperationHistoryEntry>? entries = System.Text.Json.JsonSerializer.Deserialize(
                json,
                PanosseJsonContext.Default.ListOperationHistoryEntry);
            return entries ?? new List<OperationHistoryEntry>();
        }
        catch
        {
            return new List<OperationHistoryEntry>();
        }
    }

    private void SaveNoThrow(List<OperationHistoryEntry> entries)
    {
        try
        {
            string? directory = Path.GetDirectoryName(historyPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = System.Text.Json.JsonSerializer.Serialize(
                entries,
                PanosseJsonContext.Default.ListOperationHistoryEntry);
            File.WriteAllText(historyPath, json);
        }
        catch
        {
            // Keep history non-blocking.
        }
    }
}
