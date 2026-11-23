using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.SharedLib.Events;

/// <summary>
/// Мир завершил очередной такт симуляции (на него подписываются окна для обновления).
/// </summary>
public sealed record TickCompleted(TickContext Context) : IWorldEvent;

/// <summary>
/// Модуль выбросил исключение в <c>Tick</c>. Мир продолжает работу, остальные модули не страдают.
/// </summary>
public sealed record ModuleFailed(string ModuleName, Exception Error) : IWorldEvent;

/// <summary>
/// Вид происшествия (бедствия, аварии, поломки, преступления, болезни).
/// </summary>
public enum EmergencyKind { Fire, Accident, Breakdown, NaturalDisaster, Crime, Medical }

/// <summary>
/// Произошло происшествие. Публикует модуль бедствий, реагируют службы.
/// </summary>
/// <param name="EmergencyId">Идентификатор происшествия (чтобы потом сообщить, что оно устранено).</param>
/// <param name="Kind">Вид.</param>
/// <param name="Location">Где произошло.</param>
/// <param name="Severity">Тяжесть от 1 до 5.</param>
/// <param name="SubjectId">Здание или объект, которого касается происшествие (если есть).</param>
public sealed record EmergencyReported(Guid EmergencyId, EmergencyKind Kind, Position Location, int Severity, EntityId? SubjectId = null) : IWorldEvent;

/// <summary>
/// Происшествие устранено службой.
/// </summary>
public sealed record EmergencyResolved(Guid EmergencyId) : IWorldEvent;

/// <summary>
/// Происшествие отменено.
/// </summary>
public sealed record EmergencyCancelled(Guid EmergencyId) : IWorldEvent;

/// <summary>
/// Объект изменил экологию в точке (завод ухудшает, парк улучшает).
/// Отрицательное значение — ухудшение.
/// </summary>
public sealed record EnvironmentImpact(Position Location, double Delta, string Source) : IWorldEvent;

/// <summary>
/// На карте появилось здание.
/// </summary>
public sealed record BuildingAdded(EntityId BuildingId) : IWorldEvent;

/// <summary>
/// Здание снесено.
/// </summary>
public sealed record BuildingRemoved(EntityId BuildingId) : IWorldEvent;