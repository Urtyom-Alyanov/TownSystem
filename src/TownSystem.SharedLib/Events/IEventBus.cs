namespace TownSystem.SharedLib.Events;

/// <summary>
/// Шина событий. Основной способ общения модулей друг с другом без прямых ссылок.
/// Например, модуль бедствий публикует <see cref="EmergencyReported"/>, а пожарная служба подписывается.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Публикует событие всем подписчикам его типа (и базовых типов/интерфейсов).
    /// </summary>
    void Publish(IWorldEvent worldEvent);

    /// <summary>
    /// Подписывает обработчик на события типа <typeparamref name="T"/>.
    /// Для отписки вызвать Dispose у результата.
    /// </summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : IWorldEvent;
}