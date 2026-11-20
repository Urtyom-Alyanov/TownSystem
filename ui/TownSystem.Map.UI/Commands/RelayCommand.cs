using System.Windows.Input;

namespace TownSystem.Map.UI.Commands;

/// <summary>
/// связывает кнопку с действием, которое должно запуститься
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> execute;

    // Сохраняем действие, которое потом вызовет интерфейс
    public RelayCommand(Action<object?> execute)
    {
        this.execute = execute;
    }

    // говорим WPF что эту команду можно выполнить
    public bool CanExecute(object? parameter)
    {
        return true;
    }

    // вызываем действие и передаём ему параметр из интерфейса
    public void Execute(object? parameter)
    {
        execute(parameter);
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
