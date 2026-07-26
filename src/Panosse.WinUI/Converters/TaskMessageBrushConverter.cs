using Microsoft.UI.Xaml.Data;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

namespace Panosse.WinUI.Converters;

public sealed class TaskMessageBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromArgb(255, 46, 125, 50));
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromArgb(255, 198, 40, 40));
    private static readonly SolidColorBrush DefaultBrush = new(Color.FromArgb(255, 66, 66, 66));

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string text = value as string ?? string.Empty;
        if (text.Contains('✅', StringComparison.Ordinal) ||
            text.Contains("terminé", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("terminee", StringComparison.OrdinalIgnoreCase))
        {
            return SuccessBrush;
        }

        if (text.Contains("Erreur", StringComparison.OrdinalIgnoreCase) ||
            text.Contains('❌', StringComparison.Ordinal))
        {
            return ErrorBrush;
        }

        return DefaultBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
