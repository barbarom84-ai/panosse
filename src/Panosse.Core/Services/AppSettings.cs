namespace Panosse.Services;

public sealed class AppSettings
{
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool PlaySuccessSound { get; set; } = true;
    public bool ShowTrayNotifications { get; set; } = true;
    public bool PreviewModeEnabled { get; set; }
    public string ExclusionPatterns { get; set; } = string.Empty;
    public bool EnableScheduledCleanup { get; set; }
    public int ScheduledCleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Cleanup intensity: Rapid, Standard, or Deep.
    /// </summary>
    public string CleanupProfile { get; set; } = CleanupProfiles.Standard;
}
