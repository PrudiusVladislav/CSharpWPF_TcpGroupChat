using System;
using System.Windows.Input;

namespace CSharpWPF_TcpChat.Client.Infrastructure;

public class RelayCommand : ICommand
{
    private Predicate<object?>? _canExecute;
    private Action<object?>? _execute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested+= value;
        remove => CommandManager.RequerySuggested-= value;
    }

    public RelayCommand(Action<object?>? executeMethod, Predicate<object?>? canExecuteMethod = null)
    {
        _canExecute = canExecuteMethod;
        _execute = executeMethod;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        _execute?.Invoke(parameter);
    }
}