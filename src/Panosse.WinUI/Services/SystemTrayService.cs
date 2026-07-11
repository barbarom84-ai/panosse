using System.Runtime.InteropServices;

namespace Panosse.WinUI.Services;

public sealed class SystemTrayService : ISystemTrayService
{
    private const uint WmApp = 0x8000;
    private const uint TrayMessage = WmApp + 100;
    private const uint WmCommand = 0x0111;
    private const uint WmRButtonUp = 0x0205;
    private const uint WmLButtonDblClk = 0x0203;

    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint NimAdd = 0x00000000;
    private const uint NimDelete = 0x00000002;
    private const uint NimSetVersion = 0x00000004;
    private const uint NotifyIconVersion4 = 4;

    private const uint MfString = 0x00000000;
    private const uint TpmBottomAlign = 0x0020;
    private const uint TpmLeftAlign = 0x0000;
    private const uint TpmRightButton = 0x0002;

    private const int MenuShow = 1001;
    private const int MenuCleanup = 1002;
    private const int MenuExit = 1003;
    private const int IdiApplication = 32512;

    private IntPtr hWnd = IntPtr.Zero;
    private IntPtr hIcon = IntPtr.Zero;
    private WndProcDelegate? wndProcDelegate;
    private Action? onShowRequested;
    private Action? onCleanupRequested;
    private Action? onExitRequested;
    private bool disposed;

    public void Initialize(Action onShowRequested, Action onCleanupRequested, Action onExitRequested)
    {
        ThrowIfDisposed();
        this.onShowRequested = onShowRequested;
        this.onCleanupRequested = onCleanupRequested;
        this.onExitRequested = onExitRequested;

        wndProcDelegate = WndProc;
        EnsureMessageWindow();
        AddNotifyIcon();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        RemoveNotifyIcon();

        if (hWnd != IntPtr.Zero)
        {
            _ = DestroyWindow(hWnd);
            hWnd = IntPtr.Zero;
        }

        if (hIcon != IntPtr.Zero)
        {
            _ = DestroyIcon(hIcon);
            hIcon = IntPtr.Zero;
        }

        onShowRequested = null;
        onCleanupRequested = null;
        onExitRequested = null;
        wndProcDelegate = null;
    }

    private IntPtr WndProc(IntPtr windowHandle, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == TrayMessage)
        {
            uint eventId = unchecked((uint)lParam.ToInt32());
            if (eventId == WmLButtonDblClk)
            {
                onShowRequested?.Invoke();
            }
            else if (eventId == WmRButtonUp)
            {
                ShowContextMenu(windowHandle);
            }
            return IntPtr.Zero;
        }

        if (msg == WmCommand)
        {
            int id = wParam.ToInt32() & 0xFFFF;
            switch (id)
            {
                case MenuShow:
                    onShowRequested?.Invoke();
                    break;
                case MenuCleanup:
                    onCleanupRequested?.Invoke();
                    break;
                case MenuExit:
                    onExitRequested?.Invoke();
                    break;
            }
            return IntPtr.Zero;
        }

        return DefWindowProc(windowHandle, msg, wParam, lParam);
    }

    private void ShowContextMenu(IntPtr ownerWindow)
    {
        IntPtr menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        _ = AppendMenu(menu, MfString, MenuShow, "Ouvrir Panosse");
        _ = AppendMenu(menu, MfString, MenuCleanup, "Passer la panosse");
        _ = AppendMenu(menu, MfString, MenuExit, "Quitter");

        _ = GetCursorPos(out POINT cursor);
        _ = SetForegroundWindow(ownerWindow);
        _ = TrackPopupMenuEx(
            menu,
            TpmLeftAlign | TpmBottomAlign | TpmRightButton,
            cursor.X,
            cursor.Y,
            ownerWindow,
            IntPtr.Zero);

        _ = DestroyMenu(menu);
    }

    private void EnsureMessageWindow()
    {
        string className = $"PanosseTrayWindow_{Environment.ProcessId}";
        var windowClass = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate!),
            lpszClassName = className
        };

        ushort atom = RegisterClassEx(ref windowClass);
        int lastError = Marshal.GetLastWin32Error();
        if (atom == 0 && lastError != 1410)
        {
            throw new InvalidOperationException($"Unable to register tray window class. Error={lastError}");
        }

        hWnd = CreateWindowEx(
            0,
            className,
            "PanosseTrayMessageWindow",
            0,
            0,
            0,
            0,
            0,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (hWnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Unable to create tray message window. Error={Marshal.GetLastWin32Error()}");
        }
    }

    private void AddNotifyIcon()
    {
        hIcon = LoadIcon(IntPtr.Zero, (IntPtr)IdiApplication);
        if (hIcon == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to load tray icon.");
        }

        var data = CreateNotifyData();
        if (!Shell_NotifyIcon(NimAdd, ref data))
        {
            throw new InvalidOperationException($"Unable to add tray icon. Error={Marshal.GetLastWin32Error()}");
        }

        data.uVersion = NotifyIconVersion4;
        _ = Shell_NotifyIcon(NimSetVersion, ref data);
    }

    private void RemoveNotifyIcon()
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        var data = CreateNotifyData();
        _ = Shell_NotifyIcon(NimDelete, ref data);
    }

    private NOTIFYICONDATA CreateNotifyData()
    {
        return new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = hWnd,
            uID = 1,
            uFlags = NifMessage | NifIcon | NifTip,
            uCallbackMessage = TrayMessage,
            hIcon = hIcon,
            szTip = "Panosse (WinUI)"
        };
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SystemTrayService));
        }
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle,
        string lpClassName,
        string lpWindowName,
        int dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool TrackPopupMenuEx(
        IntPtr hMenu,
        uint uFlags,
        int x,
        int y,
        IntPtr hWnd,
        IntPtr lptpm);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
