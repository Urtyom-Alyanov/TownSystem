
using TownSystem.FireService.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.FireService.Modules;

/// <summary>
/// Подключает пожарную службу к миру симуляции
/// и обрабатывает события чрезвычайных происшествий.
/// </summary>
public sealed class FireServiceModule : ISimulationModule
{
    private IWorld? _world;
    private FireServiceManagementService? _service;

    // Связь между происшествием и отправленной командой.
    private readonly Dictionary<Guid, TownSystem.SharedLib.Core.EntityId>
        _emergencyTeams = new();

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

        // Подписываемся на события происшествий.
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

        IBuildingRegistry buildingRegistry =
            _world.GetService<IBuildingRegistry>();

        _service.SetBuildingRegistry(buildingRegistry);
    }

    public void Tick(TickContext context)
    {
        // Автоматическая обработка происшествий выполняется
        // через события IEventBus.
    }

    /// <summary>
    /// Реакция на новое происшествие.
    /// </summary>
    private void HandleEmergencyReported(EmergencyReported emergency)
    {
        if (_service is null)
            return;

        // На этом этапе пожарная служба реагирует только на пожары.
        if (emergency.Kind != EmergencyKind.Fire)
            return;

        // Не отправляем команду повторно на одно происшествие.
        if (_emergencyTeams.ContainsKey(emergency.EmergencyId))
            return;

        // Ищем первую свободную команду, закреплённую за станцией.
        var team = _service.Teams.FirstOrDefault(
            team => team.IsAvailable && team.StationId is not null);

        if (team is null)
            return;

        // Отправляем команду.
        if (_service.TryDispatchTeam(team.Id))
        {
            _emergencyTeams.Add(
                emergency.EmergencyId,
                team.Id);
        }
    }

    /// <summary>
    /// Реакция на устранение происшествия.
    /// </summary>
    private void HandleEmergencyResolved(EmergencyResolved emergency)
    {
        ReleaseTeamForEmergency(emergency.EmergencyId);
    }

    /// <summary>
    /// Реакция на отмену происшествия.
    /// </summary>
    private void HandleEmergencyCancelled(EmergencyCancelled emergency)
    {
        ReleaseTeamForEmergency(emergency.EmergencyId);
    }

    /// <summary>
    /// Освобождает команду, связанную с происшествием.
    /// </summary>
    private void ReleaseTeamForEmergency(Guid emergencyId)
    {
        if (_service is null)
            return;

        if (!_emergencyTeams.TryGetValue(
                emergencyId,
                out var teamId))
        {
            return;
        }

        _service.ReleaseTeam(teamId);
        _emergencyTeams.Remove(emergencyId);
    }
}
