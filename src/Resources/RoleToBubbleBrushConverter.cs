using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OllamaDesktopChat.src.Resources;

public class RoleToBubbleBrushConverter : IValueConverter
{
    // Use the same dark gray bubble for user and assistant messages.
    private static readonly SolidColorBrush UserBrush = new(Color.FromRgb(58, 58, 58));
    private static readonly SolidColorBrush AssistantBrush = new(Color.FromRgb(58, 58, 58));
    private static readonly SolidColorBrush SystemBrush = new(Color.FromRgb(58, 58, 58));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var role = value?.ToString();
        return role switch
        {
            "user" => UserBrush,
            "assistant" => AssistantBrush,
            _ => SystemBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}