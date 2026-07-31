namespace Panosse.WinUI.Services;

public interface ISystemTrayService : IDisposable
{
    void Initialize(Action onShowRequested, Action onCleanupRequested, Action onExitRequested);

    void ShowNotification(string title, string message);
}
