using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace OllamaDesktopChat.src.Services;

public static class MarkdownDocumentRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly Brush BodyBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(170, 170, 170));
    private static readonly Brush LinkBrush = new SolidColorBrush(Color.FromRgb(14, 165, 233));
    private static readonly Brush InlineCodeBackground = new SolidColorBrush(Color.FromRgb(52, 52, 52));
    private static readonly Brush CodeBlockBackground = new SolidColorBrush(Color.FromRgb(36, 36, 36));
    private static readonly Brush QuoteBorderBrush = new SolidColorBrush(Color.FromRgb(14, 165, 233));
    private static readonly Brush QuoteBackground = new SolidColorBrush(Color.FromRgb(40, 40, 40));
    private static readonly Brush TableBorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70));

    static MarkdownDocumentRenderer()
    {
        FreezeIfPossible(BodyBrush);
        FreezeIfPossible(MutedBrush);
        FreezeIfPossible(LinkBrush);
        FreezeIfPossible(InlineCodeBackground);
        FreezeIfPossible(CodeBlockBackground);
        FreezeIfPossible(QuoteBorderBrush);
        FreezeIfPossible(QuoteBackground);
        FreezeIfPossible(TableBorderBrush);
    }

    public static FrameworkElement Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return new TextBlock
            {
                Text = string.Empty,
                Foreground = BodyBrush,
                TextWrapping = TextWrapping.Wrap
            };
        }

        var document = Markdown.Parse(markdown, Pipeline);
        var rootPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0)
        };

        foreach (var block in document)
        {
            var element = RenderBlock(block);
            if (element is not null)
            {
                rootPanel.Children.Add(element);
            }
        }

        return rootPanel;
    }

    private static FrameworkElement? RenderBlock(Markdig.Syntax.Block block)
    {
        switch (block)
        {
            case HeadingBlock heading:
                return RenderHeading(heading);
            case ParagraphBlock paragraph:
                return RenderParagraph(paragraph);
            case QuoteBlock quote:
                return RenderQuote(quote);
            case ListBlock list:
                return RenderList(list);
            case FencedCodeBlock fencedCode:
                return RenderCodeBlock(fencedCode);
            case CodeBlock codeBlock:
                return RenderCodeBlock(codeBlock);
            case ThematicBreakBlock:
                return new Border
                {
                    Height = 1,
                    Margin = new Thickness(0, 8, 0, 12),
                    Background = TableBorderBrush
                };
            case Markdig.Extensions.Tables.Table table:
                return RenderTable(table);
            case HtmlBlock html:
                return RenderHtmlBlock(html);
            case ContainerBlock container:
                return RenderContainer(container);
            default:
                return null;
        }
    }

    private static FrameworkElement RenderHeading(HeadingBlock heading)
    {
        var text = new TextBlock
        {
            Foreground = BodyBrush,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = heading.Level switch
            {
                1 => new Thickness(0, 4, 0, 12),
                2 => new Thickness(0, 2, 0, 10),
                _ => new Thickness(0, 2, 0, 8)
            },
            FontSize = heading.Level switch
            {
                1 => 30,
                2 => 24,
                3 => 20,
                4 => 18,
                _ => 16
            }
        };

        AppendInlines(text.Inlines, heading.Inline);
        return text;
    }

    private static FrameworkElement RenderParagraph(ParagraphBlock paragraph)
    {
        if (TryRenderStandaloneImage(paragraph, out var imageElement))
        {
            return imageElement;
        }

        var text = new TextBlock
        {
            Foreground = BodyBrush,
            FontSize = 14,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        AppendInlines(text.Inlines, paragraph.Inline);
        return text;
    }

    private static bool TryRenderStandaloneImage(ParagraphBlock paragraph, out FrameworkElement element)
    {
        element = null!;

        if (paragraph.Inline?.FirstChild is not LinkInline image || !image.IsImage)
        {
            return false;
        }

        if (image.NextSibling is not null)
        {
            return false;
        }

        var source = image.GetDynamicUrl is not null
            ? image.GetDynamicUrl()
            : image.Url ?? string.Empty;

        var altText = ExtractInlineText(image).Trim();
        element = RenderImage(source, altText);
        return true;
    }

    private static FrameworkElement RenderImage(string source, string altText)
    {
        var container = new Border
        {
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(0),
            Background = Brushes.Transparent
        };

        try
        {
            var uri = CreateUri(source);
            var image = new Image
            {
                MaxHeight = 360,
                Stretch = Stretch.Uniform,
                ToolTip = string.IsNullOrWhiteSpace(altText) ? source : altText,
                Source = new BitmapImage(uri)
            };

            container.Child = image;
            return container;
        }
        catch
        {
            return new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(altText)
                    ? $"[image: {source}]"
                    : $"[image: {altText}]",
                Foreground = MutedBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }
    }

    private static FrameworkElement RenderQuote(QuoteBlock quote)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        foreach (var child in quote)
        {
            var rendered = RenderBlock(child);
            if (rendered is not null)
            {
                panel.Children.Add(rendered);
            }
        }

        return new Border
        {
            Margin = new Thickness(0, 2, 0, 10),
            Padding = new Thickness(12, 8, 10, 6),
            BorderBrush = QuoteBorderBrush,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Background = QuoteBackground,
            Child = panel
        };
    }

    private static FrameworkElement RenderList(ListBlock list)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 2, 0, 10)
        };

        var index = int.TryParse(list.OrderedStart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStart)
            ? parsedStart
            : 1;
        foreach (var child in list)
        {
            if (child is not ListItemBlock listItem)
            {
                continue;
            }

            var row = new Grid
            {
                Margin = new Thickness(0, 0, 0, 4)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var marker = list.IsOrdered
                ? $"{index}."
                : "•";

            var markerText = new TextBlock
            {
                Text = marker,
                Foreground = BodyBrush,
                FontSize = 14,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            foreach (var itemChild in listItem)
            {
                var rendered = RenderBlock(itemChild);
                if (rendered is not null)
                {
                    contentPanel.Children.Add(rendered);
                }
            }

            Grid.SetColumn(markerText, 0);
            Grid.SetColumn(contentPanel, 1);
            row.Children.Add(markerText);
            row.Children.Add(contentPanel);
            panel.Children.Add(row);

            index++;
        }

        return panel;
    }

    private static FrameworkElement RenderCodeBlock(CodeBlock codeBlock)
    {
        var codeText = ExtractCodeText(codeBlock);
        var info = string.Empty;

        if (codeBlock is FencedCodeBlock fenced && fenced.Info is not null)
        {
            info = fenced.Info.Replace("`", string.Empty).Trim();
        }

        var codeTextBox = new TextBox
        {
            Text = codeText,
            IsReadOnly = true,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            AcceptsReturn = true,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = BodyBrush,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 13,
            Padding = new Thickness(0)
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        if (!string.IsNullOrWhiteSpace(info))
        {
            panel.Children.Add(new TextBlock
            {
                Text = info,
                Foreground = MutedBrush,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8),
                FontFamily = new FontFamily("Consolas")
            });
        }

        panel.Children.Add(codeTextBox);

        return new Border
        {
            Margin = new Thickness(0, 4, 0, 12),
            Padding = new Thickness(12),
            Background = CodeBlockBackground,
            BorderBrush = TableBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = panel
        };
    }

    private static FrameworkElement RenderTable(Markdig.Extensions.Tables.Table table)
    {
        var allRows = table.OfType<Markdig.Extensions.Tables.TableRow>().ToList();
        if (allRows.Count == 0)
        {
            return new TextBlock
            {
                Text = string.Empty,
                Foreground = BodyBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        var columnCount = allRows.Max(r => r.Count);
        var grid = new Grid
        {
            Margin = new Thickness(0, 4, 0, 12)
        };

        for (var c = 0; c < columnCount; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        for (var r = 0; r < allRows.Count; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var row = allRows[r];

            for (var c = 0; c < columnCount; c++)
            {
                var cell = c < row.Count ? row[c] : null;

                var cellText = new TextBlock
                {
                    Foreground = BodyBrush,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 13,
                    Margin = new Thickness(0),
                    FontWeight = r == 0 ? FontWeights.SemiBold : FontWeights.Normal
                };

                if (cell is ContainerBlock tableCell)
                {
                    foreach (var block in tableCell)
                    {
                        if (block is ParagraphBlock paragraph)
                        {
                            AppendInlines(cellText.Inlines, paragraph.Inline);
                        }
                    }
                }

                var border = new Border
                {
                    BorderBrush = TableBorderBrush,
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(10, 8, 10, 8),
                    Background = r == 0
                        ? new SolidColorBrush(Color.FromRgb(45, 45, 45))
                        : new SolidColorBrush(Color.FromRgb(37, 37, 37)),
                    Child = cellText
                };

                Grid.SetRow(border, r);
                Grid.SetColumn(border, c);
                grid.Children.Add(border);
            }
        }

        return grid;
    }

    private static FrameworkElement RenderHtmlBlock(HtmlBlock html)
    {
        var content = ExtractCodeText(html).Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return new TextBlock
            {
                Text = string.Empty,
                Foreground = BodyBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        return new Border
        {
            Margin = new Thickness(0, 2, 0, 10),
            Padding = new Thickness(10, 8, 10, 8),
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35)),
            BorderBrush = TableBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new TextBlock
            {
                Text = content,
                Foreground = MutedBrush,
                FontFamily = new FontFamily("Consolas"),
                TextWrapping = TextWrapping.Wrap
            }
        };
    }

    private static FrameworkElement? RenderContainer(ContainerBlock container)
    {
        if (container.Count == 0)
        {
            return null;
        }

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 2, 0, 10)
        };

        foreach (var child in container)
        {
            var element = RenderBlock(child);
            if (element is not null)
            {
                panel.Children.Add(element);
            }
        }

        return panel;
    }

    private static void AppendInlines(InlineCollection target, ContainerInline? inline)
    {
        if (inline is null)
        {
            return;
        }

        var current = inline.FirstChild;
        while (current is not null)
        {
            switch (current)
            {
                case LiteralInline literal:
                    target.Add(new Run(literal.Content.ToString()));
                    break;

                case LineBreakInline lineBreak:
                    target.Add(new LineBreak());
                    if (lineBreak.IsHard)
                    {
                        target.Add(new LineBreak());
                    }
                    break;

                case CodeInline code:
                    target.Add(new Run(code.Content)
                    {
                        FontFamily = new FontFamily("Consolas"),
                        Background = InlineCodeBackground,
                        Foreground = BodyBrush
                    });
                    break;

                case EmphasisInline emphasis:
                {
                    Span span = emphasis.DelimiterCount >= 2
                        ? new Bold()
                        : new Italic();

                    if (emphasis.DelimiterChar == '~')
                    {
                        span = new Span();
                        span.TextDecorations = TextDecorations.Strikethrough;
                    }

                    AppendInlines(span.Inlines, emphasis);
                    target.Add(span);
                    break;
                }

                case LinkInline link when link.IsImage:
                {
                    var source = link.GetDynamicUrl is not null
                        ? link.GetDynamicUrl()
                        : link.Url ?? string.Empty;
                    var alt = ExtractInlineText(link).Trim();
                    var image = RenderImage(source, alt);
                    target.Add(new InlineUIContainer(image));
                    break;
                }

                case LinkInline link:
                {
                    var url = link.GetDynamicUrl is not null
                        ? link.GetDynamicUrl()
                        : link.Url ?? string.Empty;

                    var hyperlink = new Hyperlink
                    {
                        Foreground = LinkBrush,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        NavigateUri = TryCreateUri(url)
                    };

                    AppendInlines(hyperlink.Inlines, link);

                    hyperlink.RequestNavigate += (_, args) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = args.Uri.AbsoluteUri,
                                UseShellExecute = true
                            });
                        }
                        catch
                        {
                            // Ignore browser launch failures to keep UI responsive.
                        }
                    };

                    target.Add(hyperlink);
                    break;
                }

                case ContainerInline container:
                    AppendInlines(target, container);
                    break;
            }

            current = current.NextSibling;
        }
    }

    private static string ExtractCodeText(LeafBlock block)
    {
        var builder = new StringBuilder();

        var lines = block.Lines;
        for (var i = 0; i < lines.Count; i++)
        {
            var slice = lines.Lines[i].Slice;
            builder.Append(slice.Text.Substring(slice.Start, slice.Length));

            if (i < lines.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static string ExtractInlineText(ContainerInline inline)
    {
        var builder = new StringBuilder();
        var current = inline.FirstChild;

        while (current is not null)
        {
            if (current is LiteralInline literal)
            {
                builder.Append(literal.Content.ToString());
            }
            else if (current is ContainerInline childContainer)
            {
                builder.Append(ExtractInlineText(childContainer));
            }

            current = current.NextSibling;
        }

        return builder.ToString();
    }

    private static Uri CreateUri(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return new Uri(value, UriKind.RelativeOrAbsolute);
    }

    private static Uri? TryCreateUri(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : null;
    }

    private static void FreezeIfPossible(Brush brush)
    {
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }
    }
}
