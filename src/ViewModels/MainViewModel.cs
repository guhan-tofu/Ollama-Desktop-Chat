using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using OllamaDesktopChat.Models;
using OllamaDesktopChat.Services;
using OllamaDesktopChat.src.Models;
using OllamaDesktopChat.src.Services;

namespace OllamaDesktopChat.src.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly OllamaService _ollamaService;
    private readonly ConversationManager _conversationManager;
    private readonly FileUploadService _fileUploadService;
    private CancellationTokenSource? _activeRequestCts;
    private OllamaModel? _selectedModel;
    private string _userInput = string.Empty;
    private bool _isGenerating;
    private string _statusMessage = "Ready";
    private Guid _currentConversationId = Guid.NewGuid();
    private string _searchQuery = string.Empty;
    private string? _manualConversationTitle;

    public MainViewModel(OllamaService ollamaService)
    {
        _ollamaService = ollamaService;
        _conversationManager = new ConversationManager();
        _fileUploadService = new FileUploadService();

        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, CanSend);
        ClearChatCommand = new RelayCommand(ClearChat);
        RefreshModelsCommand = new AsyncRelayCommand(RefreshModelsAsync, () => !IsGenerating);
        LoadConversationCommand = new AsyncRelayCommand<Guid>(LoadConversationAsync);
        NewConversationCommand = new RelayCommand(CreateNewConversation);
        DeleteConversationCommand = new AsyncRelayCommand<Guid>(DeleteConversationAsync);
        RenameConversationCommand = new RelayCommand<Guid>(BeginInlineRename);
        UploadFileCommand = new AsyncRelayCommand(UploadFileAsync);

        _ = RefreshModelsAsync();
        _ = LoadConversationsAsync();
    }

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ObservableCollection<OllamaModel> Models { get; } = new();

    public ObservableCollection<ConversationMetadata> Conversations { get; } = new();

    public ObservableCollection<AttachmentMetadata> PendingAttachments { get; } = new();

    public Guid CurrentConversationId
    {
        get => _currentConversationId;
        set
        {
            if (_currentConversationId == value)
            {
                return;
            }

            _currentConversationId = value;
            OnPropertyChanged();
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery == value)
            {
                return;
            }

            _searchQuery = value;
            OnPropertyChanged();
            _ = FilterAndRefreshConversationsAsync();
        }
    }

    public OllamaModel? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (_selectedModel == value)
            {
                return;
            }

            _selectedModel = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public string UserInput
    {
        get => _userInput;
        set
        {
            if (_userInput == value)
            {
                return;
            }

            _userInput = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public bool IsGenerating
    {
        get => _isGenerating;
        private set
        {
            if (_isGenerating == value)
            {
                return;
            }

            _isGenerating = value;
            OnPropertyChanged();
            RaiseCommandStates();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ICommand SendMessageCommand { get; }

    public ICommand ClearChatCommand { get; }

    public ICommand RefreshModelsCommand { get; }

    public ICommand LoadConversationCommand { get; }

    public ICommand NewConversationCommand { get; }

    public ICommand DeleteConversationCommand { get; }

    public ICommand RenameConversationCommand { get; }

    public ICommand UploadFileCommand { get; }

    public void CancelActiveRequest()
    {
        _activeRequestCts?.Cancel();
    }

    private async Task RefreshModelsAsync()
    {
        try
        {
            StatusMessage = "Loading models...";

            var models = await _ollamaService.GetModelsAsync();
            var previousSelection = SelectedModel?.Name;

            Models.Clear();
            foreach (var model in models.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
            {
                Models.Add(model);
            }

            SelectedModel = Models.FirstOrDefault(m => string.Equals(m.Name, previousSelection, StringComparison.OrdinalIgnoreCase))
                ?? Models.FirstOrDefault();

            if (Models.Count == 0)
            {
                StatusMessage = "No Ollama models found. Install one with: ollama pull <model>.";
                AddSystemMessage("No models found. Pull at least one model in Ollama and refresh.");
                return;
            }

            StatusMessage = $"Loaded {Models.Count} model(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to load models.";
            AddSystemMessage(ex.Message);
        }
    }

    private async Task SendMessageAsync()
    {
        if (!CanSend())
        {
            return;
        }

        if (SelectedModel is null)
        {
            AddSystemMessage("Select a model before sending a message.");
            return;
        }

        var input = UserInput.Trim();
        UserInput = string.Empty;

        var userMessage = new ChatMessage
        {
            Role = "user",
            Content = input,
            Timestamp = DateTime.Now
        };

        Messages.Add(userMessage);

        var assistantMessage = new ChatMessage
        {
            Role = "assistant",
            Content = string.Empty,
            Timestamp = DateTime.Now
        };

        Messages.Add(assistantMessage);

        var contextMessages = Messages
            .Where(m => m.Role is "user" or "assistant")
            .Take(Messages.Count - 1)
            .Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp
            })
            .ToList();

        _activeRequestCts = new CancellationTokenSource();

        try
        {
            IsGenerating = true;
            StatusMessage = $"Generating response with {SelectedModel.Name}...";

            await _ollamaService.StreamMessageAsync(
                SelectedModel.Name,
                contextMessages,
                token =>
                {
                    if (Application.Current?.Dispatcher != null)
                    {
                        // Apply token updates synchronously so persistence always sees the final assistant text.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            assistantMessage.Content += token;
                        }, DispatcherPriority.Background);
                    }
                    else
                    {
                        assistantMessage.Content += token;
                    }
                },
                _activeRequestCts.Token);

            StatusMessage = "Ready";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Request canceled.";
            AddSystemMessage("The active request was canceled.");
        }
        catch (Exception ex)
        {
            StatusMessage = "Generation failed.";
            assistantMessage.Content = "";
            Messages.Remove(assistantMessage);
            AddSystemMessage(ex.Message);
        }
        finally
        {
            IsGenerating = false;
            _activeRequestCts?.Dispose();
            _activeRequestCts = null;

            // Persist conversation after message exchange
            _ = PersistCurrentConversationAsync();
        }
    }

    private void ClearChat()
    {
        Messages.Clear();
        StatusMessage = "Conversation cleared.";
    }

    private void AddSystemMessage(string content)
    {
        Messages.Add(new ChatMessage
        {
            Role = "system",
            Content = content,
            Timestamp = DateTime.Now
        });
    }

    private bool CanSend()
    {
        return !IsGenerating && !string.IsNullOrWhiteSpace(UserInput) && SelectedModel is not null;
    }

    /// <summary>
    /// Loads all saved conversations from disk and updates the Conversations collection.
    /// </summary>
    private async Task LoadConversationsAsync()
    {
        try
        {
            var conversations = await _conversationManager.ListConversationsAsync();
            Conversations.Clear();
            foreach (var conversation in conversations)
            {
                Conversations.Add(conversation);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading conversations: {ex.Message}");
        }
    }

    /// <summary>
    /// Filters conversations by search query and refreshes the Conversations collection.
    /// </summary>
    private async Task FilterAndRefreshConversationsAsync()
    {
        try
        {
            var allConversations = await _conversationManager.ListConversationsAsync();

            var filtered = string.IsNullOrWhiteSpace(SearchQuery)
                ? allConversations
                : allConversations.Where(c => c.Title?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            Conversations.Clear();
            foreach (var conversation in filtered)
            {
                Conversations.Add(conversation);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error filtering conversations: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads a conversation from disk by ID and displays it in the chat area.
    /// </summary>
    private async Task LoadConversationAsync(Guid conversationId)
    {
        try
        {
            StatusMessage = "Loading conversation...";
            var conversationData = await _conversationManager.LoadConversationAsync(conversationId);

            if (conversationData == null)
            {
                AddSystemMessage("Conversation not found.");
                return;
            }

            // Update current conversation ID
            CurrentConversationId = conversationId;

            // Clear and load messages
            Messages.Clear();
            if (conversationData.Messages != null)
            {
                foreach (var message in conversationData.Messages)
                {
                    Messages.Add(message);
                }
            }

            // Restore selected model
            if (!string.IsNullOrEmpty(conversationData.SelectedModel))
            {
                SelectedModel = Models.FirstOrDefault(m => m.Name == conversationData.SelectedModel)
                    ?? Models.FirstOrDefault();
            }

            _manualConversationTitle = conversationData.Title;

            // Auto-scroll to bottom (this will be handled by MainWindow.xaml.cs)
            StatusMessage = $"Loaded conversation: {conversationData.Title}";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to load conversation.";
            AddSystemMessage($"Error loading conversation: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new conversation session (clears messages and generates new ID).
    /// </summary>
    private void CreateNewConversation()
    {
        try
        {
            CancelAllInlineRenames();
            CurrentConversationId = Guid.NewGuid();
            _manualConversationTitle = null;
            Messages.Clear();
            UserInput = string.Empty;
            PendingAttachments.Clear();
            StatusMessage = "New conversation started.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to create new conversation.";
            AddSystemMessage($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes a conversation from disk.
    /// </summary>
    private async Task DeleteConversationAsync(Guid conversationId)
    {
        try
        {
            StatusMessage = "Deleting conversation...";
            CancelAllInlineRenames();
            await _conversationManager.DeleteConversationAsync(conversationId);

            // If we deleted the current conversation, start a new one
            if (conversationId == CurrentConversationId)
            {
                CreateNewConversation();
            }

            // Refresh the conversations list
            await LoadConversationsAsync();
            StatusMessage = "Conversation deleted.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to delete conversation.";
            AddSystemMessage($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Enters inline rename mode for a conversation row.
    /// </summary>
    private void BeginInlineRename(Guid conversationId)
    {
        foreach (var item in Conversations)
        {
            item.IsRenaming = false;
        }

        var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);
        if (conversation == null)
        {
            return;
        }

        conversation.EditableTitle = conversation.Title;
        conversation.IsRenaming = true;
    }

    public async Task CommitInlineRenameAsync(Guid conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);
        if (conversation == null || !conversation.IsRenaming)
        {
            return;
        }

        try
        {
            var newTitle = conversation.EditableTitle?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(newTitle))
            {
                conversation.EditableTitle = conversation.Title;
                conversation.IsRenaming = false;
                return;
            }

            if (string.Equals(newTitle, conversation.Title, StringComparison.Ordinal))
            {
                conversation.IsRenaming = false;
                return;
            }

            await _conversationManager.RenameConversationAsync(conversationId, newTitle);

            conversation.Title = newTitle;
            conversation.EditableTitle = newTitle;
            conversation.LastModified = DateTime.Now;
            conversation.IsRenaming = false;

            if (conversationId == CurrentConversationId)
            {
                _manualConversationTitle = newTitle;
            }

            await LoadConversationsAsync();
            StatusMessage = "Conversation renamed.";
        }
        catch (Exception ex)
        {
            conversation.IsRenaming = false;
            StatusMessage = "Failed to rename conversation.";
            AddSystemMessage($"Error: {ex.Message}");
        }
    }

    public void CancelInlineRename(Guid conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);
        if (conversation == null)
        {
            return;
        }

        conversation.EditableTitle = conversation.Title;
        conversation.IsRenaming = false;
    }

    private void CancelAllInlineRenames()
    {
        foreach (var conversation in Conversations)
        {
            conversation.EditableTitle = conversation.Title;
            conversation.IsRenaming = false;
        }
    }

    /// <summary>
    /// Persists the current conversation (messages, model selection, etc.) to disk.
    /// </summary>
    private async Task PersistCurrentConversationAsync()
    {
        try
        {
            var title = string.IsNullOrWhiteSpace(_manualConversationTitle)
                ? GenerateConversationTitle()
                : _manualConversationTitle;
            var model = SelectedModel?.Name ?? "unknown";

            await _conversationManager.SaveConversationAsync(
                CurrentConversationId,
                title,
                Messages.ToList(),
                model);

            // Refresh conversations list
            await LoadConversationsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error persisting conversation: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a title for the current conversation from the first user message.
    /// </summary>
    private string GenerateConversationTitle()
    {
        var firstUserMessage = Messages.FirstOrDefault(m => m.Role == "user");
        if (firstUserMessage == null)
        {
            return $"Conversation {DateTime.Now:yyyy-MM-dd HH:mm}";
        }

        var title = firstUserMessage.Content.Replace("\n", " ").Trim();
        if (title.Length > 30)
        {
            title = title.Substring(0, 27) + "...";
        }

        return title;
    }

    /// <summary>
    /// Handles file upload: validates, stores, and adds to pending attachments.
    /// </summary>
    private async Task UploadFileAsync()
    {
        try
        {
            // This would be called from UI with a file dialog
            // For now, we provide the infrastructure; UI binding will handle file selection
            StatusMessage = "Ready to upload file.";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusMessage = "Upload failed.";
            AddSystemMessage($"Error uploading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Uploads a specific file and adds it to pending attachments.
    /// </summary>
    public async Task UploadFileInternalAsync(string filePath)
    {
        try
        {
            StatusMessage = "Uploading file...";
            var metadata = await _fileUploadService.UploadFileAsync(filePath);

            if (metadata != null)
            {
                PendingAttachments.Add(metadata);
                StatusMessage = $"File uploaded: {metadata.FileName}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Upload failed.";
            AddSystemMessage($"Error uploading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes an attachment from pending attachments (before sending).
    /// </summary>
    public async Task RemovePendingAttachmentAsync(Guid attachmentId)
    {
        try
        {
            var attachment = PendingAttachments.FirstOrDefault(a => a.Id == attachmentId);
            if (attachment != null)
            {
                PendingAttachments.Remove(attachment);
                await _fileUploadService.DeleteAttachmentAsync(attachment.FilePath);
                StatusMessage = "Attachment removed.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to remove attachment.";
            AddSystemMessage($"Error: {ex.Message}");
        }
    }

    private void RaiseCommandStates()
    {
        if (SendMessageCommand is AsyncRelayCommand sendCommand)
        {
            sendCommand.RaiseCanExecuteChanged();
        }

        if (RefreshModelsCommand is AsyncRelayCommand refreshCommand)
        {
            refreshCommand.RaiseCanExecuteChanged();
        }
    }
}