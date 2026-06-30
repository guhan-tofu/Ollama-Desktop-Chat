using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OllamaDesktopChat.src.Models;

public class ChatMessage : INotifyPropertyChanged
{
    private string _role = "user";
    private string _content = string.Empty;

    public string Role
    {
        get => _role;
        set
        {
            if (_role == value)
            {
                return;
            }

            _role = value;
            OnPropertyChanged();
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            if (_content == value)
            {
                return;
            }

            _content = value;
            OnPropertyChanged();
        }
    }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Unique identifier for the conversation this message belongs to.
    /// </summary>
    public Guid ConversationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// List of file attachments (images, text files) associated with this message.
    /// </summary>
    public List<OllamaDesktopChat.Models.AttachmentMetadata> Attachments { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}