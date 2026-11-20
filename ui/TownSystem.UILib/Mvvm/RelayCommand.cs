using System.Windows.Input;

namespace TownSystem.UILib.Mvvm;

/// <summary>
/// Команда из делегатов.
/// <c>new RelayCommand(Save, () =&gt; CanSave)</c>.
/// </summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => execute();
}

/// <summary
/// >Команда с параметром из XAML (<c>CommandParameter</c>).
/// </summary>
public sealed class RelayCommand<T>(Action<T?> execute, Predicate<T?>? canExecute = null) : ICommand
{
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter is T t ? t : default) ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => execute(parameter is T t ? t : default);
}
