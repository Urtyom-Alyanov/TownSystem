using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TownSystem.Map.UI.ViewModels;

/// <summary>
/// Общая основа для классов, которые сообщают экрану об изменениях данных
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // говорим интерфейсу что свойство поменялось и его пора обновить
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}