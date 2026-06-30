using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OllamaDesktopChat.Models;
using OllamaDesktopChat.src.Models;

namespace OllamaDesktopChat.Services
{
    /// <summary>
    /// Manages persistence of conversations to/from disk.
    /// Stores conversations as JSON files in %AppData%/OllamaDesktopChat/conversations/
    /// </summary>
    public class ConversationManager
    {
        private readonly string _conversationsFolder;
        private readonly JsonSerializerOptions _jsonOptions;

        public ConversationManager()
        {
            // Initialize conversations folder path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _conversationsFolder = Path.Combine(appDataPath, "OllamaDesktopChat", "conversations");

            // Create folder if it doesn't exist
            Directory.CreateDirectory(_conversationsFolder);

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }

        /// <summary>
        /// Saves a conversation with its messages to disk.
        /// Creates a JSON file with filename: {conversationId}.json
        /// </summary>
        /// <param name="conversationId">Unique identifier for conversation</param>
        /// <param name="title">Display title for conversation</param>
        /// <param name="messages">List of chat messages in conversation</param>
        /// <param name="selectedModel">Name of Ollama model selected for this conversation</param>
        public async Task SaveConversationAsync(Guid conversationId, string title, List<ChatMessage> messages, string selectedModel)
        {
            try
            {
                var conversationData = new ConversationData
                {
                    ConversationId = conversationId,
                    Title = title ?? GenerateTitleFromMessages(messages),
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    SelectedModel = selectedModel,
                    Messages = messages ?? new List<ChatMessage>()
                };

                var filePath = Path.Combine(_conversationsFolder, $"{conversationId}.json");
                var json = JsonSerializer.Serialize(conversationData, _jsonOptions);

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving conversation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads a conversation from disk by ID.
        /// </summary>
        /// <param name="conversationId">Unique identifier for conversation</param>
        /// <returns>Conversation data, or null if not found</returns>
        public async Task<ConversationData> LoadConversationAsync(Guid conversationId)
        {
            try
            {
                var filePath = Path.Combine(_conversationsFolder, $"{conversationId}.json");

                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var conversationData = JsonSerializer.Deserialize<ConversationData>(json, _jsonOptions);

                return conversationData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading conversation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lists all saved conversations with their metadata.
        /// </summary>
        /// <returns>List of conversation metadata sorted by last modified (newest first)</returns>
        public async Task<List<ConversationMetadata>> ListConversationsAsync()
        {
            try
            {
                var conversationFiles = Directory.GetFiles(_conversationsFolder, "*.json");
                var conversations = new List<ConversationMetadata>();

                foreach (var filePath in conversationFiles)
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var data = JsonSerializer.Deserialize<ConversationData>(json, _jsonOptions);

                    if (data != null)
                    {
                        conversations.Add(new ConversationMetadata
                        {
                            ConversationId = data.ConversationId,
                            Title = data.Title,
                            CreatedAt = data.CreatedAt,
                            LastModified = data.LastModified,
                            SelectedModel = data.SelectedModel,
                            MessageCount = data.Messages?.Count ?? 0,
                            PreviewText = ExtractPreviewText(data.Messages)
                        });
                    }
                }

                // Return sorted by last modified (newest first)
                return conversations.OrderByDescending(c => c.LastModified).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error listing conversations: {ex.Message}");
                return new List<ConversationMetadata>();
            }
        }

        /// <summary>
        /// Deletes a conversation from disk.
        /// </summary>
        /// <param name="conversationId">Unique identifier for conversation</param>
        public async Task DeleteConversationAsync(Guid conversationId)
        {
            try
            {
                var filePath = Path.Combine(_conversationsFolder, $"{conversationId}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting conversation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Renames a conversation title on disk.
        /// </summary>
        /// <param name="conversationId">Unique identifier for conversation</param>
        /// <param name="newTitle">New title text</param>
        public async Task RenameConversationAsync(Guid conversationId, string newTitle)
        {
            try
            {
                var filePath = Path.Combine(_conversationsFolder, $"{conversationId}.json");
                if (!File.Exists(filePath))
                {
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var conversationData = JsonSerializer.Deserialize<ConversationData>(json, _jsonOptions);
                if (conversationData == null)
                {
                    return;
                }

                conversationData.Title = string.IsNullOrWhiteSpace(newTitle)
                    ? conversationData.Title
                    : newTitle.Trim();
                conversationData.LastModified = DateTime.Now;

                var updatedJson = JsonSerializer.Serialize(conversationData, _jsonOptions);
                await File.WriteAllTextAsync(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error renaming conversation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates a title from the first user message in a conversation.
        /// Truncates to 30 characters.
        /// </summary>
        private string GenerateTitleFromMessages(List<ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return $"Conversation {Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var firstUserMessage = messages.FirstOrDefault(m => m.Role == "user");
            if (firstUserMessage == null)
            {
                return $"Conversation {Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var title = firstUserMessage.Content.Replace("\n", " ").Trim();
            if (title.Length > 30)
            {
                title = title.Substring(0, 27) + "...";
            }

            return title;
        }

        /// <summary>
        /// Extracts preview text from messages for sidebar display.
        /// Returns the last message content (up to 50 characters).
        /// </summary>
        private string ExtractPreviewText(List<ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return "(Empty conversation)";
            }

            var lastMessage = messages.Last();
            var preview = lastMessage.Content.Replace("\n", " ").Trim();

            if (preview.Length > 50)
            {
                preview = preview.Substring(0, 47) + "...";
            }

            return preview;
        }

        /// <summary>
        /// Internal model for JSON serialization of conversation data.
        /// </summary>
        public class ConversationData
        {
            public Guid ConversationId { get; set; }
            public string Title { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastModified { get; set; }
            public string SelectedModel { get; set; }
            public List<ChatMessage> Messages { get; set; }
        }
    }
}
