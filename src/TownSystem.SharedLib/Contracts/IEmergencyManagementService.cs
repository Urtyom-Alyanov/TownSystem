using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Emergencies;
using TownSystem.SharedLib.Events;

namespace TownSystem.SharedLib.Contracts;

/// <summary>
/// Интерфейс управления чрезвычайными происшествиями.
/// </summary>
public interface IEmergencyManagementService
{
    /// <summary>
    /// Все зарегистрированные происшествия.
    /// </summary>
    IReadOnlyCollection<EmergencyIncident> Incidents { get; }

    EmergencyIncident Report(
        string name,
        EmergencyKind kind,
        Position location,
        int severity,
        EntityId? subjectId = null);

    /// <summary>
    /// Находит происшествие по идентификатору.
    /// </summary>
    EmergencyIncident? FindById(EntityId id);

    /// <summary>
    /// Начинает обработку происшествия.
    /// </summary>
    bool StartResponse(EntityId id);

    /// <summary>
    /// Завершает происшествие.
    /// </summary>
    bool Resolve(EntityId id);

    /// <summary>
    /// Отменяет происшествие.
    /// </summary>
    bool Cancel(EntityId id);
}