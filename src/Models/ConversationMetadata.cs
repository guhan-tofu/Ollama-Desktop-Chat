using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OllamaDesktopChat.Models
{
    /// <summary>
    /// Metadata for a conversation, used for display in the sidebar.
    /// </summary>
    public class ConversationMetadata : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private DateTime _lastModified = DateTime.Now;
        private string _selectedModel = string.Empty;
        private string _previewText = string.Empty;
        private bool _isRenaming;
        private string _editableTitle = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Unique identifier for this conversation.
        /// </summary>
        public Guid ConversationId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display title for the conversation (auto-generated from first user message or user-defined).
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (_title == value)
                {
                    return;
                }

                _title = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Timestamp when conversation was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Timestamp when conversation was last modified (message added or edited).
        /// </summary>
        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                if (_lastModified == value)
                {
                    return;
                }

                _lastModified = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Name of the Ollama model selected for this conversation.
        /// </summary>
        public string SelectedModel
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
            }
        }

        /// <summary>
        /// Number of messages in this conversation.
        /// </summary>
        public int MessageCount { get; set; }

        /// <summary>
        /// Preview text: first 50 characters of last message for sidebar display.
        /// </summary>
        public string PreviewText
        {
            get => _previewText;
            set
            {
                if (_previewText == value)
                {
                    return;
                }

                _previewText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// True when this conversation row is in inline rename mode.
        /// </summary>
        public bool IsRenaming
        {
            get => _isRenaming;
            set
            {
                if (_isRenaming == value)
                {
                    return;
                }

                _isRenaming = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Editable title text used by inline rename textbox.
        /// </summary>
        public string EditableTitle
        {
            get => _editableTitle;
            set
            {
                if (_editableTitle == value)
                {
                    return;
                }

                _editableTitle = value;
                OnPropertyChanged();
            }
        }
    }
}
