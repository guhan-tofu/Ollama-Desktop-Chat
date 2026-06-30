using System.Globalization;
using System.Windows.Data;

namespace OllamaDesktopChat.src.Resources;

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool flag && !flag;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool flag && !flag;
    }
}