
using TownSystem.FireService.Models;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.FireService.Services;

public sealed class FireServiceManagementService
    : IFireServiceManagementService
{
    private IBuildingRegistry? _buildingRegistry;

    private readonly Dictionary<EntityId, FireStation> _stations = new();
    private readonly Dictionary<EntityId, RescueTeam> _teams = new();

    public FireServiceManagementService()
    {
    }

    /// <summary>
    /// Подключает общий реестр зданий после инициализации мира.
    /// </summary>
    internal void SetBuildingRegistry(IBuildingRegistry buildingRegistry)
    {
        ArgumentNullException.ThrowIfNull(buildingRegistry);

        if (_buildingRegistry is not null)
            throw new InvalidOperationException(
                "Реестр зданий уже подключён.");

        _buildingRegistry = buildingRegistry;
    }

    public IReadOnlyCollection<FireStation> Stations =>
        _stations.Values.ToArray();

    public IReadOnlyCollection<RescueTeam> Teams =>
        _teams.Values.ToArray();

    public bool TryRegisterStation(FireStation station)
    {
        ArgumentNullException.ThrowIfNull(station);

        if (_stations.ContainsKey(station.Id))
            return false;

        if (_buildingRegistry is null)
            throw new InvalidOperationException(
                "Реестр зданий ещё не подключён.");

        if (!_buildingRegistry.TryAdd(station))
            return false;

        _stations.Add(station.Id, station);
        return true;
    }
    public bool ApplyStationDamage(EntityId stationId, double amount)
    {
        if (!_stations.TryGetValue(stationId, out FireStation? station))
            return false;

        station.ApplyDamage(amount);
        return true;
    }

    public bool RepairStation(EntityId stationId, double amount)
    {
        if (!_stations.TryGetValue(stationId, out FireStation? station))
            return false;

        station.Repair(amount);
        return true;
    }

    public bool TryAssignTeam(EntityId stationId, RescueTeam team)
    {
        ArgumentNullException.ThrowIfNull(team);

        if (!_stations.TryGetValue(stationId, out FireStation? station))
            return false;

        if (_teams.ContainsKey(team.Id))
            return false;

        if (team.StationId is not null && team.StationId != stationId)
            return false;

        if (!station.IsOperational)
            return false;

        int currentMembers = _teams.Values
            .Where(t => t.StationId == stationId)
            .Sum(t => t.Members.Count);

        if (currentMembers + team.Members.Count > station.Capacity)
            return false;

        var existingFirefighterIds = _teams.Values
            .SelectMany(t => t.Members)
            .Select(member => member.Id)
            .ToHashSet();

        if (team.Members.Any(member =>
                existingFirefighterIds.Contains(member.Id)))
        {
            return false;
        }

        team.AssignToStation(stationId);
        _teams.Add(team.Id, team);

        return true;
    }

    public bool TryAddFirefighterToTeam(
    EntityId teamId,
    Firefighter firefighter)
    {
        ArgumentNullException.ThrowIfNull(firefighter);

        if (!_teams.TryGetValue(teamId, out RescueTeam? team))
            return false;

        if (team.Status != RescueTeamStatus.Available)
            return false;

        // Проверяем, что сотрудник ещё не состоит в другой команде.
        bool alreadyAssigned = _teams.Values
            .SelectMany(existingTeam => existingTeam.Members)
            .Any(member => member.Id == firefighter.Id);

        if (alreadyAssigned)
            return false;

        // Проверяем вместимость станции.
        if (team.StationId is EntityId stationId)
        {
            if (!_stations.TryGetValue(
                    stationId,
                    out FireStation? station))
                return false;

            long currentMembers = _teams.Values
                .Where(existingTeam =>
                    existingTeam.StationId == stationId)
                .Sum(existingTeam => existingTeam.Members.Count);

            if (currentMembers + 1 > station.Capacity)
                return false;
        }

        return team.TryAddMember(firefighter);
    }

    public bool TryRemoveFirefighterFromTeam(
        EntityId teamId,
        EntityId firefighterId)
    {
        if (!_teams.TryGetValue(teamId, out RescueTeam? team))
            return false;

        return team.TryRemoveMember(firefighterId);
    }

    /// <summary>
    /// Обрабатывает удаление пожарной станции из общего реестра.
    /// </summary>
    internal bool RemoveStation(EntityId stationId)
    {
        if (!_stations.Remove(stationId))
            return false;

        foreach (var team in _teams.Values.Where(
                     team => team.StationId == stationId))
        {
            // Команды, которые не находятся на выезде,
            // освобождаются от привязки к станции.
            team.TryUnassignFromStation();
        }

        return true;
    }

    public bool TryDispatchTeam(EntityId teamId)
    {
        if (!_teams.TryGetValue(teamId, out RescueTeam? team))
            return false;

        if (team.StationId is null)
            return false;

        if (!_stations.TryGetValue(
                team.StationId.Value,
                out FireStation? station))
            return false;

        if (!station.IsOperational)
            return false;

        return team.TryDispatch();
    }
    public bool SetFirefighterDuty(
    EntityId firefighterId,
    bool isOnDuty)
    {
        Firefighter? firefighter = _teams.Values
            .SelectMany(team => team.Members)
            .FirstOrDefault(member => member.Id == firefighterId);

        if (firefighter is null)
            return false;

        return firefighter.TrySetOnDuty(isOnDuty);
    }

    public bool ReleaseTeam(EntityId teamId)
    {
        if (!_teams.TryGetValue(teamId, out RescueTeam? team))
            return false;

        if (team.Status != RescueTeamStatus.Dispatched)
            return false;

        team.Release();

        // Если станцию снесли во время выезда,
        // после возвращения команда отвязывается от неё.
        if (team.StationId is EntityId stationId &&
            !_stations.ContainsKey(stationId))
        {
            team.TryUnassignFromStation();
        }

        return true;
    }

    public FireStation? FindStation(EntityId id)
    {
        return _stations.GetValueOrDefault(id);
    }

    public RescueTeam? FindTeam(EntityId id)
    {
        return _teams.GetValueOrDefault(id);
    }
}
