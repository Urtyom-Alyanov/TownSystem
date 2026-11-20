using System.Diagnostics;
using System.Windows.Threading;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

namespace TownSystem.UILib.Services;

/// <summary>
/// Гоняет <see cref="World.Update"/> по таймеру UI-потока (~20 раз в секунду). Всё в одном потоке,
/// поэтому модулям и ViewModel не нужна синхронизация. Если какой-то модуль станет слишком тяжёлым,
/// его надо оптимизировать, а не выносить в поток.
/// </summary>
public sealed class SimulationRunner : ObservableObject
{
    private readonly World _world;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private bool _isRunning;

    /// <summary>
    /// Создаёт запуск симуляции (по умолчанию остановлен).
    /// </summary>
    public SimulationRunner(World world)
    {
        _world = world;
        _timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) =>
        {
            var elapsed = _stopwatch.Elapsed;
            _stopwatch.Restart();
            _world.Update(elapsed);
        };
    }

    /// <summary>Идёт ли симуляция.</summary>
    public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }

    /// <summary>Запускает или продолжает симуляцию.</summary>
    public void Start()
    {
        if (IsRunning) return;
        _stopwatch.Restart();
        _timer.Start();
        IsRunning = true;
    }

    /// <summary>Останавливает симуляцию (состояние мира сохраняется).</summary>
    public void Stop()
    {
        if (!IsRunning) return;
        _timer.Stop();
        _stopwatch.Stop();
        IsRunning = false;
    }
}
