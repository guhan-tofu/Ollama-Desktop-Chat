using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OllamaDesktopChat.src.Resources;

public class RoleToHorizontalAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var role = value?.ToString();
        return role switch
        {
            "user" => HorizontalAlignment.Right,
            "assistant" => HorizontalAlignment.Left,
            _ => HorizontalAlignment.Center
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}