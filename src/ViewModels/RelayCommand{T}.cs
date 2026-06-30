using System;
using System.Windows.Input;

namespace OllamaDesktopChat.src.ViewModels;

/// <summary>
/// Generic relay command that executes a synchronous Action and supports parameter passing.
/// </summary>
/// <typeparam name="T">Type of command parameter</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (parameter is T typedParameter)
        {
            return _canExecute?.Invoke(typedParameter) ?? true;
        }

        return _canExecute?.Invoke(default) ?? true;
    }

    public void Execute(object? parameter)
    {
        T? typedParameter = parameter is T tp ? tp : default;
        _execute(typedParameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
