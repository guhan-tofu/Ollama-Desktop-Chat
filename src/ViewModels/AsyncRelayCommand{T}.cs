using System;
using System.Windows.Input;
using System.Windows;

namespace OllamaDesktopChat.src.ViewModels;

/// <summary>
/// Generic async relay command that executes an async Task and supports parameter passing.
/// </summary>
/// <typeparam name="T">Type of command parameter</typeparam>
public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
            return false;

        if (parameter is T typedParameter)
        {
            return _canExecute?.Invoke(typedParameter) ?? true;
        }

        // If parameter doesn't match type T, try default
        return _canExecute?.Invoke(default) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        T? typedParameter = parameter is T tp ? tp : default;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute(typedParameter);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
