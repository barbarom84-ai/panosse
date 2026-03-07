namespace Panosse.Services;

public sealed class AppSettings
{
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool PlaySuccessSound { get; set; } = true;
    public bool ShowTrayNotifications { get; set; } = true;
}
