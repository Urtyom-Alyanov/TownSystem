using TownSystem.SharedLib.Events;

namespace TownSystem.SharedLib.Simulation;

/// <summary>
/// Реализация <see cref="IWorld"/>.
/// Держит модули и прогоняет такты.
/// </summary>
public sealed class World : IWorld
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly List<ISimulationModule> _modules = new();
    private bool _ready;

    /// <inheritdoc />
    public SimulationClock Clock { get; } = new();

    /// <inheritdoc />
    public IEventBus Events { get; } = new EventBus();

    /// <inheritdoc />
    public IReadOnlyList<ISimulationModule> Modules => _modules;

    /// <summary>
    /// Регистрирует модуль и вызывает его <see cref="ISimulationModule.Initialize"/>.
    /// </summary>
    public void RegisterModule(ISimulationModule module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (_modules.Any(m => m.Name == module.Name))
            throw new InvalidOperationException($"Модуль '{module.Name}' уже зарегистрирован.");

        module.Initialize(this);
        _modules.Add(module);
        _modules.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    /// <summary>
    /// Сообщает всем модулям, что регистрация закончена.
    /// Вызвать один раз после всех <see cref="RegisterModule"/>.
    /// </summary>
    public void Start()
    {
        if (_ready) return;
        _ready = true;
        foreach (var module in _modules.ToArray())
            module.OnWorldReady();
    }

    /// <summary>
    /// Продвигает мир на реальный интервал. Ошибка в одном модуле не останавливает остальные:
    /// публикуется <see cref="ModuleFailed"/>.
    /// </summary>
    /// <param name="realDelta">Реальный промежуток времени</param>
    public void Update(TimeSpan realDelta)
    {
        var delta = Clock.Advance(realDelta);
        if (delta == TimeSpan.Zero) return;

        var context = new TickContext(delta, Clock.Now);
        foreach (var module in _modules.ToArray())
        {
            try { module.Tick(context); }
            catch (Exception ex) { Events.Publish(new ModuleFailed(module.Name, ex)); }
        }

        Events.Publish(new TickCompleted(context));
    }

    /// <inheritdoc />
    public void RegisterService<T>(T service) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        _services[typeof(T)] = service;
    }

    /// <inheritdoc />
    public T GetService<T>() where T : class =>
        TryGetService<T>(out var service)
            ? service!
            : throw new InvalidOperationException($"Сервис {typeof(T).Name} не зарегистрирован. Проверьте, что нужный модуль добавлен и его Order меньше.");

    /// <inheritdoc />
    public bool TryGetService<T>(out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj)) { service = (T)obj; return true; }
        service = null;
        return false;
    }
}