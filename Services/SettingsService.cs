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
                return new AppSettings();
            }

            string json = File.ReadAllText(settingsPath);
            AppSettings? settings = System.Text.Json.JsonSerializer.Deserialize(
                json,
                PanosseJsonContext.Default.AppSettings);
            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
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
}
