using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OllamaDesktopChat.Models;

namespace OllamaDesktopChat.Services
{
    /// <summary>
    /// Manages file uploads and storage for chat attachments.
    /// Validates file types and stores files in %AppData%/OllamaDesktopChat/attachments/
    /// </summary>
    public class FileUploadService
    {
        private readonly string _attachmentsFolder;
        private readonly string[] _validImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private readonly string[] _validTextExtensions = { ".txt", ".md", ".markdown", ".json", ".xml", ".csv", ".cs", ".ts", ".js", ".py", ".java", ".cpp", ".c", ".h", ".html", ".css", ".sql" };
        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB

        public FileUploadService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _attachmentsFolder = Path.Combine(appDataPath, "OllamaDesktopChat", "attachments");

            Directory.CreateDirectory(_attachmentsFolder);
        }

        /// <summary>
        /// Validates and uploads a file to the attachments folder.
        /// </summary>
        /// <param name="filePath">Full path to file to upload</param>
        /// <returns>AttachmentMetadata if successful, or null if validation fails</returns>
        public async Task<AttachmentMetadata> UploadFileAsync(string filePath)
        {
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var fileInfo = new FileInfo(filePath);

                // Validate file size
                if (fileInfo.Length > MaxFileSizeBytes)
                {
                    throw new InvalidOperationException($"File size exceeds 20MB limit. File size: {fileInfo.Length} bytes");
                }

                // Determine file type and validate extension
                var extension = Path.GetExtension(filePath).ToLower();
                var fileType = DetermineFileType(extension);

                if (fileType == null)
                {
                    throw new InvalidOperationException($"File type not supported: {extension}");
                }

                // Generate unique filename to avoid conflicts
                var uniqueFileName = $"{Guid.NewGuid()}_{fileInfo.Name}";
                var destinationPath = Path.Combine(_attachmentsFolder, uniqueFileName);

                // Copy file to attachments folder
                await Task.Run(() => File.Copy(filePath, destinationPath, overwrite: false));

                // Create and return metadata
                var metadata = new AttachmentMetadata
                {
                    Id = Guid.NewGuid(),
                    FileName = fileInfo.Name,
                    FilePath = uniqueFileName,
                    FileType = fileType,
                    UploadedAt = DateTime.Now,
                    MimeType = GetMimeType(extension),
                    FileSizeBytes = fileInfo.Length
                };

                return metadata;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error uploading file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the full path to a stored attachment file.
        /// </summary>
        /// <param name="relativeFilePath">Relative filename in attachments folder</param>
        /// <returns>Full path to file</returns>
        public string GetAttachmentPath(string relativeFilePath)
        {
            return GetAttachmentPathStatic(relativeFilePath);
        }

        /// <summary>
        /// Gets the full path to a stored attachment file (static version for external use).
        /// </summary>
        /// <param name="relativeFilePath">Relative filename in attachments folder</param>
        /// <returns>Full path to file</returns>
        public static string GetAttachmentPathStatic(string relativeFilePath)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var attachmentsFolder = Path.Combine(appDataPath, "OllamaDesktopChat", "attachments");
            return Path.Combine(attachmentsFolder, relativeFilePath);
        }

        /// <summary>
        /// Deletes an attachment file from storage.
        /// </summary>
        /// <param name="relativeFilePath">Relative filename in attachments folder</param>
        public async Task DeleteAttachmentAsync(string relativeFilePath)
        {
            try
            {
                var fullPath = GetAttachmentPath(relativeFilePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting attachment: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines file type (image or text) based on extension.
        /// </summary>
        private string DetermineFileType(string extension)
        {
            if (_validImageExtensions.Contains(extension))
            {
                return "image";
            }

            if (_validTextExtensions.Contains(extension))
            {
                return "text";
            }

            return null;
        }

        /// <summary>
        /// Gets MIME type for a file extension.
        /// </summary>
        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".txt" => "text/plain",
                ".md" or ".markdown" => "text/markdown",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".csv" => "text/csv",
                ".cs" => "text/x-csharp",
                ".ts" => "text/typescript",
                ".js" => "text/javascript",
                ".py" => "text/x-python",
                ".java" => "text/x-java",
                ".cpp" => "text/x-c++src",
                ".c" => "text/x-csrc",
                ".h" => "text/x-csrc",
                ".html" => "text/html",
                ".css" => "text/css",
                ".sql" => "text/x-sql",
                _ => "application/octet-stream"
            };
        }
    }
}
