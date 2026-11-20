using TownSystem.SharedLib.Events;

namespace TownSystem.SharedLib.Simulation;

/// <summary>
/// Мир города.
/// Это, что видит каждый модуль.
/// Хранит часы, шину событий и реестр общих сервисов.
/// </summary>
public interface IWorld
{
    /// <summary>
    /// Игровые часы.
    /// </summary>
    SimulationClock Clock { get; }

    /// <summary>
    /// Шина событий.
    /// </summary>
    IEventBus Events { get; }

    /// <summary>
    /// Зарегистрированные модули в порядке тактов.
    /// </summary>
    IReadOnlyList<ISimulationModule> Modules { get; }

    /// <summary>
    /// Публикует сервис для других модулей по типу (обычно интерфейс из <c>Contracts</c>),
    /// например <c>world.RegisterService&lt;IMapService&gt;(map)</c>.
    /// </summary>
    void RegisterService<T>(T service) where T : class;

    /// <summary>Возвращает сервис или бросает <see cref="InvalidOperationException"/>, если его никто не зарегистрировал.</summary>
    T GetService<T>() where T : class;

    /// <summary>Возвращает сервис, если он есть. Удобно для необязательных зависимостей.</summary>
    bool TryGetService<T>(out T? service) where T : class;
}