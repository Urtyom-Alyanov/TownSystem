using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TownSystem.UILib.Mvvm;

/// <summary>
/// Базовый класс с уведомлением об изменении свойств (<see cref="INotifyPropertyChanged"/>).
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Сообщает привязкам, что свойство изменилось.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Присваивает значение полю и уведомляет, если оно изменилось.
    /// </summary>
    /// <param name="field">Поле</param>
    /// <param name="value">Значение</param>
    /// <param name="name">Имя события</param>
    /// <returns>Возвращает true при изменении.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
