using System.Globalization;
using System.Windows.Data;
using OllamaDesktopChat.src.Services;

namespace OllamaDesktopChat.src.Resources;

public class AssistantMarkdownToUiConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var markdown = value?.ToString() ?? string.Empty;
        return MarkdownDocumentRenderer.Render(markdown);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
