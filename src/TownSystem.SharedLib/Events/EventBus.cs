namespace TownSystem.SharedLib.Events;

/// <summary>
/// Простая потокобезопасная реализация <see cref="IEventBus"/>.
/// </summary>
public sealed class EventBus : IEventBus
{
    private sealed record Handler(Type EventType, Action<IWorldEvent> Invoke);

    private readonly object _lock = new();
    private readonly List<Handler> _handlers = new();

    /// <inheritdoc />
    public void Publish(IWorldEvent worldEvent)
    {
        ArgumentNullException.ThrowIfNull(worldEvent);
        var runtimeType = worldEvent.GetType();

        Handler[] snapshot;
        lock (_lock)
            snapshot = _handlers.Where(h => h.EventType.IsAssignableFrom(runtimeType)).ToArray();

        foreach (var handler in snapshot)
            handler.Invoke(worldEvent);
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Action<T> handler) where T : IWorldEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Handler entry = new(typeof(T), e => handler((T)e));
        lock (_lock) _handlers.Add(entry);
        return new Subscription(() => { lock (_lock) _handlers.Remove(entry); });
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        private Action? _onDispose = onDispose;
        public void Dispose() => Interlocked.Exchange(ref _onDispose, null)?.Invoke();
    }
}