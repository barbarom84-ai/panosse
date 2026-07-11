using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Panosse.WinUI.Services;

/// <summary>
/// Calcule une taille de fenêtre adaptée à la zone de travail et au facteur DPI courant.
/// Cible les résolutions fréquentes (1366×768, 1920×1080, 2560×1440) avec échelles 100–200 %.
/// </summary>
internal static class DisplayLayoutHelper
{
    // Taille de référence à 100 % (pixels effectifs XAML).
    private const double BaseWidth = 540;
    private const double BaseHeight = 880;

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

        int maxWidth = Math.Max((int)Math.Round(400 * scale), workArea.Width - horizontalMargin);
        int maxHeight = Math.Max((int)Math.Round(620 * scale), workArea.Height - verticalMargin);

        int minWidth = (int)Math.Round(480 * scale);
        int minHeight = (int)Math.Round(700 * scale);

        width = Math.Clamp(width, minWidth, maxWidth);
        height = Math.Clamp(height, minHeight, maxHeight);

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
