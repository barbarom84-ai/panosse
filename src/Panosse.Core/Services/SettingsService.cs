using System;
using System.IO;

namespace Panosse.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly string settingsPath;

    public SettingsService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "Panosse");
        settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return CreateDefaultSettings();
            }

            string json = File.ReadAllText(settingsPath);
            AppSettings? settings = System.Text.Json.JsonSerializer.Deserialize(
                json,
                PanosseJsonContext.Default.AppSettings);
            AppSettings loaded = settings ?? CreateDefaultSettings();
            if (MigrateIfNeeded(loaded))
            {
                Save(loaded);
            }

            return loaded;
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
            string? directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = System.Text.Json.JsonSerializer.Serialize(
                settings,
                PanosseJsonContext.Default.AppSettings);
            File.WriteAllText(settingsPath, json);
        }
        catch
        {
            // Rester silencieux pour ne pas bloquer l'app.
        }
    }

    internal static bool MigrateIfNeeded(AppSettings settings)
    {
        int from = settings.SchemaVersion;
        if (from >= AppSettings.CurrentSchemaVersion)
        {
            // Still normalize volatile fields for safety.
            Normalize(settings);
            return false;
        }

        // v0 -> v1: clamp schedule, normalize profile.
        if (from < 1)
        {
            Normalize(settings);
            settings.SchemaVersion = 1;
        }

        // v1 -> v2: introduce UiScalePercent default.
        if (from < 2)
        {
            if (settings.UiScalePercent is not (100 or 110 or 125))
            {
                settings.UiScalePercent = 100;
            }

            settings.SchemaVersion = 2;
        }

        Normalize(settings);
        settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
        return true;
    }

    private static void Normalize(AppSettings settings)
    {
        if (settings.ScheduledCleanupIntervalHours < 1)
        {
            settings.ScheduledCleanupIntervalHours = 24;
        }

        settings.CleanupProfile = CleanupProfiles.Normalize(settings.CleanupProfile);
        settings.ExclusionPatterns ??= string.Empty;
        if (settings.UiScalePercent is not (100 or 110 or 125))
        {
            settings.UiScalePercent = 100;
        }
    }

    private static AppSettings CreateDefaultSettings() =>
        new()
        {
            SchemaVersion = AppSettings.CurrentSchemaVersion
        };
}
