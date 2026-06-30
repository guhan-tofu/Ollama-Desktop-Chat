using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace OllamaDesktopChat.src.Services
{
    /// <summary>
    /// Converts markdown-formatted text to WPF Inline elements for rich text display.
    /// Supports: bold, italic, inline code, code blocks, line breaks, and links.
    /// </summary>
    public class MarkdownConverter
    {
        private static readonly SolidColorBrush CodeBlockBackground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51)); // #333333
        private static readonly SolidColorBrush CodeTextColor = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240)); // #f0f0f0
        private static readonly SolidColorBrush LinkColor = new SolidColorBrush(Color.FromArgb(255, 14, 165, 233)); // #0ea5e9

        /// <summary>
        /// Converts markdown text to WPF Inline elements.
        /// </summary>
        public static IEnumerable<Inline> Convert(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return Array.Empty<Inline>();

            var inlines = new List<Inline>();
            var lines = markdown.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);

            bool inCodeBlock = false;
            string codeBlockContent = "";

            foreach (var line in lines)
            {
                // Check for code block markers (``` or ~~~)
                if (line.TrimStart().StartsWith("```") || line.TrimStart().StartsWith("~~~"))
                {
                    if (inCodeBlock)
                    {
                        // End of code block - add as a special formatted run with code block styling
                        inlines.Add(new LineBreak());
                        var codeBlockRun = new Run(codeBlockContent.TrimEnd('\n', '\r'))
                        {
                            FontFamily = new System.Windows.Media.FontFamily("Courier New, Consolas"),
                            FontSize = 12,
                            Foreground = CodeTextColor,
                            Background = CodeBlockBackground
                        };
                        inlines.Add(codeBlockRun);
                        inlines.Add(new LineBreak());
                        inCodeBlock = false;
                        codeBlockContent = "";
                    }
                    else
                    {
                        // Start of code block
                        inCodeBlock = true;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    codeBlockContent += line + "\n";
                }
                else
                {
                    // Process inline markdown in regular lines
                    var lineInlines = ProcessInlineMarkdown(line);
                    inlines.AddRange(lineInlines);
                    inlines.Add(new LineBreak());
                }
            }

            // Close unclosed code block if any
            if (inCodeBlock && !string.IsNullOrEmpty(codeBlockContent))
            {
                inlines.Add(new LineBreak());
                var codeBlockRun = new Run(codeBlockContent.TrimEnd('\n', '\r'))
                {
                    FontFamily = new System.Windows.Media.FontFamily("Courier New, Consolas"),
                    FontSize = 12,
                    Foreground = CodeTextColor,
                    Background = CodeBlockBackground
                };
                inlines.Add(codeBlockRun);
                inlines.Add(new LineBreak());
            }

            return inlines;
        }

        private static List<Inline> ProcessInlineMarkdown(string text)
        {
            var inlines = new List<Inline>();
            int pos = 0;

            while (pos < text.Length)
            {
                // Try to match bold (**text**)
                var boldMatch = Regex.Match(text.Substring(pos), @"^\*\*(.+?)\*\*");
                if (boldMatch.Success)
                {
                    inlines.Add(new Bold(new Run(boldMatch.Groups[1].Value)));
                    pos += boldMatch.Length;
                    continue;
                }

                // Try to match italic (*text* or _text_)
                var italicMatch = Regex.Match(text.Substring(pos), @"^(\*|_)(.+?)\1");
                if (italicMatch.Success)
                {
                    inlines.Add(new Italic(new Run(italicMatch.Groups[2].Value)));
                    pos += italicMatch.Length;
                    continue;
                }

                // Try to match inline code (`code`)
                var codeMatch = Regex.Match(text.Substring(pos), @"^`(.+?)`");
                if (codeMatch.Success)
                {
                    inlines.Add(CreateInlineCodeRun(codeMatch.Groups[1].Value));
                    pos += codeMatch.Length;
                    continue;
                }

                // Try to match link [text](url)
                var linkMatch = Regex.Match(text.Substring(pos), @"^\[(.+?)\]\((.+?)\)");
                if (linkMatch.Success)
                {
                    var hyperlink = new Hyperlink(new Run(linkMatch.Groups[1].Value))
                    {
                        NavigateUri = new Uri(linkMatch.Groups[2].Value),
                        Foreground = LinkColor
                    };
                    hyperlink.RequestNavigate += (s, e) =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = e.Uri.AbsoluteUri,
                            UseShellExecute = true
                        });
                        e.Handled = true;
                    };
                    inlines.Add(hyperlink);
                    pos += linkMatch.Length;
                    continue;
                }

                // No special formatting found, add regular character
                inlines.Add(new Run(text[pos].ToString()));
                pos++;
            }

            return inlines;
        }

        private static Run CreateInlineCodeRun(string code)
        {
            var run = new Run(code)
            {
                Foreground = CodeTextColor,
                FontFamily = new System.Windows.Media.FontFamily("Courier New, Consolas"),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)) // #3c3c3c
            };
            return run;
        }
    }
}
