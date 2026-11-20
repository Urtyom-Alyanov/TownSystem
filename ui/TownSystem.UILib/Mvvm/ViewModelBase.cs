namespace TownSystem.UILib.Mvvm;

/// <summary>
/// Базовая ViewModel любого окна или вкладки.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private string _title = string.Empty;
    private bool _isBusy;

    /// <summary>
    /// Заголовок окна или вкладки.
    /// </summary>
    public string Title { get => _title; set => SetProperty(ref _title, value); }

    /// <summary>
    /// Идёт длительная операция (можно привязать индикатор).
    /// </summary>
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
}