using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.UILib.Mvvm;

/// <summary>
/// Базовая ViewModel вкладки модуля. Сама подписывается на такты мира и раз в
/// <see cref="RefreshEveryTicks"/> тактов вызывает <see cref="Refresh"/>, чтобы данные на экране оживали.
/// Симуляция работает в UI-потоке (см. <c>SimulationRunner</c>), поэтому обновлять ObservableCollection можно напрямую.
/// </summary>
public abstract class ModuleViewModel : ViewModelBase, IDisposable
{
    private readonly IDisposable _subscription;
    private int _ticksSinceRefresh;

    /// <summary>
    /// Создаёт ViewModel вкладки.
    /// </summary>
    protected ModuleViewModel(IWorld world, string title)
    {
        World = world;
        Title = title;
        _subscription = world.Events.Subscribe<TickCompleted>(_ =>
        {
            if (++_ticksSinceRefresh < RefreshEveryTicks) return;
            _ticksSinceRefresh = 0;
            Refresh();
        });
    }

    /// <summary>
    /// Мир, с которым работает вкладка.
    /// </summary>
    protected IWorld World { get; }

    /// <summary>
    /// Как часто обновлять данные (в тактах). Увеличьте, если окно тяжёлое.
    /// </summary>
    protected virtual int RefreshEveryTicks => 5;

    /// <summary>
    /// Перечитать данные из модели и обновить свойства/коллекции.
    /// </summary>
    public abstract void Refresh();

    /// <inheritdoc />
    public void Dispose() => _subscription.Dispose();
}
