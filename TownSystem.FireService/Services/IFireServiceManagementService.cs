
using TownSystem.FireService.Models;
using TownSystem.SharedLib.Core;

namespace TownSystem.FireService.Services;

/// <summary>
/// Управление пожарными станциями и пожарно-спасательными командами.
/// </summary>
public interface IFireServiceManagementService
{
    /// <summary>
    /// Зарегистрированные пожарные станции.
    /// </summary>
    IReadOnlyCollection<FireStation> Stations { get; }

    /// <summary>
    /// Зарегистрированные пожарно-спасательные команды.
    /// </summary>
    IReadOnlyCollection<RescueTeam> Teams { get; }

    /// <summary>
    /// Регистрирует пожарную станцию в общем реестре зданий.
    /// </summary>
    bool TryRegisterStation(FireStation station);

    /// <summary>
    /// Закрепляет команду за пожарной станцией.
    /// </summary>
    bool TryAssignTeam(EntityId stationId, RescueTeam team);

    /// <summary>
    /// Отправляет свободную команду на происшествие.
    /// </summary>
    bool TryDispatchTeam(EntityId teamId);

    /// <summary>
    /// Освобождает команду после завершения вызова.
    /// </summary>
    bool ReleaseTeam(EntityId teamId);

    /// <summary>
    /// Наносит повреждения пожарной станции.
    /// </summary>
    bool ApplyStationDamage(EntityId stationId, double amount);

    /// <summary>
    /// Ремонтирует пожарную станцию.
    /// </summary>
    bool RepairStation(EntityId stationId, double amount);

    /// <summary>
    /// Изменяет состояние дежурства сотрудника.
    /// </summary>
    bool SetFirefighterDuty(EntityId firefighterId, bool isOnDuty);

    /// <summary>
    /// Находит пожарную станцию.
    /// </summary>
    FireStation? FindStation(EntityId id);

    /// <summary>
    /// Находит пожарную команду.
    /// </summary>
    RescueTeam? FindTeam(EntityId id);
}
