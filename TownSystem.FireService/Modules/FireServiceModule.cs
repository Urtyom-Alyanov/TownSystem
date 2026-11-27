using TownSystem.FireService.Models;
using TownSystem.FireService.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.FireService.Modules;

public sealed class FireServiceModule : ISimulationModule
{
    private IWorld? _world;
    private FireServiceManagementService? _service;

    private readonly Dictionary<Guid, EntityId> _emergencyTeams = new();

    private readonly PriorityQueue<
    EmergencyReported,
    (int Severity, long Order)> _pendingEmergencies = new();

    private long _nextEmergencyOrder;

    private readonly HashSet<Guid> _pendingEmergencyIds = new();

    private IDisposable? _reportedSubscription;
    private IDisposable? _resolvedSubscription;
    private IDisposable? _cancelledSubscription;

    public string Name => "Fire Service";

    public int Order => 0;

    public void Initialize(IWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _service = new FireServiceManagementService();

        world.RegisterService<IFireServiceManagementService>(_service);

        _reportedSubscription =
            world.Events.Subscribe<EmergencyReported>(
                HandleEmergencyReported);

        _resolvedSubscription =
            world.Events.Subscribe<EmergencyResolved>(
                HandleEmergencyResolved);

        _cancelledSubscription =
            world.Events.Subscribe<EmergencyCancelled>(
                HandleEmergencyCancelled);
    }

    public void OnWorldReady()
    {
        if (_world is null || _service is null)
            throw new InvalidOperationException(
                "Пожарная служба не была инициализирована.");

        IBuildingRegistry registry =
            _world.GetService<IBuildingRegistry>();

        _service.SetBuildingRegistry(registry);
    }

    public void Tick(TickContext context)
    {
        DispatchPendingEmergencies();
    }

    private void HandleEmergencyReported(EmergencyReported emergency)
    {
        if (_service is null ||
            emergency.Kind != EmergencyKind.Fire)
            return;

        if (_emergencyTeams.ContainsKey(emergency.EmergencyId) ||
            !_pendingEmergencyIds.Add(emergency.EmergencyId))
            return;

        _pendingEmergencies.Enqueue(
        emergency,
        (-emergency.Severity, _nextEmergencyOrder++));

        DispatchPendingEmergencies();
    }

    /// <summary>
    /// Распределяет ожидающие вызовы между свободными командами.
    /// </summary>
    private void DispatchPendingEmergencies()
    {
        if (_service is null)
            return;

        while (_pendingEmergencies.Count > 0)
        {
            EmergencyReported emergency = _pendingEmergencies.Peek();

            // Пропускаем уже отменённые или завершённые вызовы.
            if (!_pendingEmergencyIds.Contains(emergency.EmergencyId))
            {
                _pendingEmergencies.Dequeue();
                continue;
            }

            // Сначала рассматриваем команды ближайших станций.
            var candidates = _service.Teams
                .Where(team =>
                    team.IsAvailable &&
                    team.StationId.HasValue)
                .Select(team => new
                {
                    Team = team,
                    Station = _service.FindStation(
                        team.StationId!.Value)
                })
                .Where(item => item.Station is not null)
                .OrderBy(item =>
                    item.Station!.Position.DistanceTo(emergency.Location))
                .ToArray();

            Models.RescueTeam? selectedTeam = null;

            foreach (var candidate in candidates)
            {
                if (_service.TryDispatchTeam(candidate.Team.Id))
                {
                    selectedTeam = candidate.Team;
                    break;
                }
            }

            // Нет свободных команд — оставляем вызов в очереди.
            if (selectedTeam is null)
                return;

            _pendingEmergencies.Dequeue();
            _pendingEmergencyIds.Remove(emergency.EmergencyId);

            _emergencyTeams[emergency.EmergencyId] = selectedTeam.Id;
        }
    }

    private void HandleEmergencyResolved(EmergencyResolved emergency)
    {
        ReleaseTeamForEmergency(emergency.EmergencyId);
    }

    private void HandleEmergencyCancelled(EmergencyCancelled emergency)
    {
        ReleaseTeamForEmergency(emergency.EmergencyId);
    }

    private void ReleaseTeamForEmergency(Guid emergencyId)
    {
        if (_service is null)
            return;

        if (_emergencyTeams.Remove(emergencyId, out EntityId teamId))
        {
            _service.ReleaseTeam(teamId);
        }

        // Удаляем ожидающий вызов, если команда ещё не была назначена.
        _pendingEmergencyIds.Remove(emergencyId);

        // Освободившаяся команда может принять следующий вызов.
        DispatchPendingEmergencies();
    }
}