namespace Panosse.WinUI.Services;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool Register(IntPtr hWnd);
}
