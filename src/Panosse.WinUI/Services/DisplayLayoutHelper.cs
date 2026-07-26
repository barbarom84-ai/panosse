using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Panosse.WinUI.Services;

/// <summary>
/// Calcule une taille de fenêtre adaptée à la zone de travail et au facteur DPI courant.
/// Taille de référence compacte type PC Manager (~720×680), clampée à la zone utile.
/// </summary>
internal static class DisplayLayoutHelper
{
    // Taille de référence à 100 % (pixels effectifs XAML).
    private const double BaseWidth = 720;
    private const double BaseHeight = 680;

    public static SizeInt32 CalculateWindowSize(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        DisplayArea displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;

        double scale = 1.0;
        if (window.Content is FrameworkElement root && root.XamlRoot is not null)
        {
            scale = root.XamlRoot.RasterizationScale;
        }

        scale = Math.Clamp(scale, 1.0, 2.5);

        int width = (int)Math.Round(BaseWidth * scale);
        int height = (int)Math.Round(BaseHeight * scale);

        int horizontalMargin = (int)Math.Round(48 * scale);
        int verticalMargin = (int)Math.Round(56 * scale);

        int maxWidth = Math.Max((int)Math.Round(600 * scale), workArea.Width - horizontalMargin);
        int maxHeight = Math.Max((int)Math.Round(520 * scale), workArea.Height - verticalMargin);

        int minWidth = (int)Math.Round(660 * scale);
        int minHeight = (int)Math.Round(600 * scale);

        width = Math.Clamp(width, Math.Min(minWidth, maxWidth), maxWidth);
        height = Math.Clamp(height, Math.Min(minHeight, maxHeight), maxHeight);

        return new SizeInt32(width, height);
    }

    public static void ApplyWindowSize(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(CalculateWindowSize(window));
    }
}
