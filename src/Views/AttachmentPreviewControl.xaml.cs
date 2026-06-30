using System.Windows;
using System.Windows.Controls;
using OllamaDesktopChat.Models;
using OllamaDesktopChat.Services;

namespace OllamaDesktopChat.src.Views
{
    /// <summary>
    /// AttachmentPreviewControl.xaml code-behind
    /// Displays preview of uploaded file attachments (images or text files)
    /// </summary>
    public partial class AttachmentPreviewControl : UserControl
    {
        public AttachmentPreviewControl()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (DataContext is not AttachmentMetadata metadata)
                return;

            // Format file size
            FileSizeText.Text = metadata.FileSizeBytes > 0 
                ? FormatFileSize(metadata.FileSizeBytes) 
                : "";

            // Show image thumbnail for image types
            if (metadata.FileType == "image")
            {
                ImageThumbnail.Visibility = Visibility.Visible;
                FileIcon.Visibility = Visibility.Collapsed;
                RemoveButton.Click += (s, e) =>
                {
                    // Raise event or call parent handler to remove this attachment
                    if (Parent is Panel panel)
                    {
                        panel.Children.Remove(this);
                    }
                };
            }
            else if (metadata.FileType == "text")
            {
                // For text files, show preview of content
                FileIcon.Text = "📝";
                FileIcon.Visibility = Visibility.Visible;
                ImageThumbnail.Visibility = Visibility.Collapsed;

                // Try to load and display preview (max 100 chars)
                try
                {
                    string fullPath = FileUploadService.GetAttachmentPathStatic(metadata.FilePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        string content = System.IO.File.ReadAllText(fullPath);
                        PreviewText.Text = content.Length > 100 
                            ? content.Substring(0, 100) + "..." 
                            : content;
                        PreviewText.Visibility = Visibility.Visible;
                    }
                }
                catch
                {
                    // Silently fail if preview can't be loaded
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
