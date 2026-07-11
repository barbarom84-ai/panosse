using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Panosse.WinUI.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isVisible = value is true;
        if (parameter is string mode && mode.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is Visibility visibility && visibility == Visibility.Visible;
}
