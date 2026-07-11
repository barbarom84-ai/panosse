using System.Runtime.InteropServices;

namespace Panosse.WinUI.Services;

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int HotkeyId = 9001;
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint VkP = 0x50;
    private const int WmHotkey = 0x0312;
    private const int GwlWndproc = -4;

    private IntPtr hWnd;
    private IntPtr originalWndProc;
    private WndProcDelegate? wndProcDelegate;
    private bool disposed;

    public event EventHandler? HotkeyPressed;

    public bool Register(IntPtr hWnd)
    {
        ThrowIfDisposed();
        this.hWnd = hWnd;
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        wndProcDelegate = WndProc;
        IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProcDelegate);
        originalWndProc = SetWindowLongPtr(hWnd, GwlWndproc, newWndProcPtr);
        return RegisterHotKey(hWnd, HotkeyId, ModControl | ModAlt, VkP);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (hWnd != IntPtr.Zero)
        {
            UnregisterHotKey(hWnd, HotkeyId);
            if (originalWndProc != IntPtr.Zero)
            {
                _ = SetWindowLongPtr(hWnd, GwlWndproc, originalWndProc);
            }
        }
        hWnd = IntPtr.Zero;
        originalWndProc = IntPtr.Zero;
        wndProcDelegate = null;
    }

    private IntPtr WndProc(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return IntPtr.Zero;
        }

        return CallWindowProc(originalWndProc, windowHandle, message, wParam, lParam);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GlobalHotkeyService));
        }
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : SetWindowLong32(hWnd, nIndex, dwNewLong);
    }
}
