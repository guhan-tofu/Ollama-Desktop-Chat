using System;

namespace OllamaDesktopChat.Models
{
    /// <summary>
    /// Metadata for file attachments (images, text files) associated with chat messages.
    /// </summary>
    public class AttachmentMetadata
    {
        /// <summary>
        /// Unique identifier for this attachment.
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Original filename as uploaded by user.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Relative path to stored file (relative to %AppData%/OllamaDesktopChat/attachments/).
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File type: "image" (jpg, png, webp) or "text" (txt, md, json, code).
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Timestamp when file was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// MIME type of the file (e.g., "image/png", "text/plain").
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long FileSizeBytes { get; set; }
    }
}
