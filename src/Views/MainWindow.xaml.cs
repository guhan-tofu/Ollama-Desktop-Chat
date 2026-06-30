using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using OllamaDesktopChat.Models;
using OllamaDesktopChat.src.Services;
using OllamaDesktopChat.src.ViewModels;
using OllamaDesktopChat.Views;

namespace OllamaDesktopChat.src.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(new OllamaService());
        DataContext = _viewModel;

        _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
        Closing += OnWindowClosing;
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScrollToLatestMessage();
    }

    private void ScrollToLatestMessage()
    {
        if (ChatListBox.Items.Count == 0 || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (ChatListBox.Items.Count > 0)
            {
                ChatListBox.ScrollIntoView(ChatListBox.Items[^1]);
            }
        }, DispatcherPriority.Background);
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.CancelActiveRequest();
        _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
    }

    private void ConversationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConversationsListBox.SelectedItem is not ConversationMetadata conversation)
        {
            return;
        }

        if (_viewModel.LoadConversationCommand.CanExecute(conversation.ConversationId))
        {
            _viewModel.LoadConversationCommand.Execute(conversation.ConversationId);
        }
    }

    private void ConversationOptionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
        e.Handled = true;
    }

    private async void ConversationRenameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not ConversationMetadata conversation)
        {
            return;
        }

        await _viewModel.CommitInlineRenameAsync(conversation.ConversationId);
    }

    private async void ConversationRenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.DataContext is not ConversationMetadata conversation)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await _viewModel.CommitInlineRenameAsync(conversation.ConversationId);
            return;
        }

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            _viewModel.CancelInlineRename(conversation.ConversationId);
            return;
        }
    }

    private void ConversationRenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox || textBox.Visibility != Visibility.Visible)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Input);
    }

    private async void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a file to attach",
            Filter = "Supported files|*.png;*.jpg;*.jpeg;*.webp;*.txt|All files|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            await _viewModel.UploadFileInternalAsync(dialog.FileName);
        }
    }

    private void MessageInputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
        var isEnter = key == Key.Enter || key == Key.Return;

        if (!isEnter)
        {
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            return;
        }

        e.Handled = true;
        if (_viewModel.SendMessageCommand.CanExecute(null))
        {
            _viewModel.SendMessageCommand.Execute(null);

            if (sender is TextBox input)
            {
                Dispatcher.BeginInvoke(() => input.Focus(), DispatcherPriority.Background);
            }
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        settingsWindow.ShowDialog();
    }
}