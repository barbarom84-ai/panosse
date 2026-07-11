using System;
using System.IO;

namespace Panosse.Services;

public sealed class LoggingService : ILoggerService
{
    private readonly object syncRoot = new();
    private readonly string logPath;

    public LoggingService()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataPath, "Panosse");
        logPath = Path.Combine(appFolder, "panosse.log");
    }

    public void LogInfo(string category, string message) => Write("INFO", category, message, null);

    public void LogWarning(string category, string message) => Write("WARN", category, message, null);

    public void LogError(string category, string message, Exception? exception = null) => Write("ERROR", category, message, exception);

    private void Write(string level, string category, string message, Exception? exception)
    {
        try
        {
            string? directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string details = exception == null
                ? string.Empty
                : $" | ex={exception.GetType().Name}: {exception.Message}";

            string line = $"[{DateTime.UtcNow:O}] [{level}] [{category}] {message}{details}";

            lock (syncRoot)
            {
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Never fail app behavior because of logging.
        }
    }
}
