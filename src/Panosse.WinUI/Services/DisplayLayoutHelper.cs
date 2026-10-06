using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace Panosse.WinUI.Services;

/// <summary>
/// Calcule une taille de fenêtre adaptée à la zone de travail, au DPI et à l'échelle UI utilisateur.
/// Taille de référence confortable (~1060×820) pour que les pages riches (Pilotes, Historique) tiennent
/// sans texte ni bouton tronqué, clampée à la zone utile.
/// </summary>
internal static class DisplayLayoutHelper
{
    // Taille de référence à 100 % (pixels effectifs XAML).
    private const double BaseWidth = 1060;
    private const double BaseHeight = 820;

    // En dessous, la barre latérale et les cartes de 620 px ne tiennent plus côte à côte.
    private const double MinimumWidth = 940;
    private const double MinimumHeight = 680;

    /// <summary>
    /// User preference multiplier (1.0, 1.1, 1.25). Combined with DPI scale.
    /// </summary>
    public static double UserScale { get; set; } = 1.0;

    public static SizeInt32 CalculateWindowSize(Window window) => CalculateSizes(window).Preferred;

    public static void ApplyWindowSize(Window window)
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(window);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
        (SizeInt32 preferred, SizeInt32 minimum) = CalculateSizes(window);

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = minimum.Width;
            presenter.PreferredMinimumHeight = minimum.Height;
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                return;
            }
        }

        appWindow.Resize(preferred);
    }

    public static void ApplyUserScalePercent(int percent)
    {
        UserScale = percent switch
        {
            110 => 1.10,
            125 => 1.25,
            _ => 1.0
        };
    }

    private static (SizeInt32 Preferred, SizeInt32 Minimum) CalculateSizes(Window window)
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

        scale = Math.Clamp(scale, 1.0, 2.5) * Math.Clamp(UserScale, 1.0, 1.25);

        int maxWidth = workArea.Width - (int)Math.Round(32 * scale);
        int maxHeight = workArea.Height - (int)Math.Round(32 * scale);

        int minWidth = Math.Min((int)Math.Round(MinimumWidth * scale), maxWidth);
        int minHeight = Math.Min((int)Math.Round(MinimumHeight * scale), maxHeight);

        int width = Math.Clamp((int)Math.Round(BaseWidth * scale), minWidth, Math.Max(minWidth, maxWidth));
        int height = Math.Clamp((int)Math.Round(BaseHeight * scale), minHeight, Math.Max(minHeight, maxHeight));

        return (new SizeInt32(width, height), new SizeInt32(minWidth, minHeight));
    }
}
