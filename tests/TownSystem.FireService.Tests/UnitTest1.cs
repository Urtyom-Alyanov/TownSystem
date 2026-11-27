
using TownSystem.FireService.Models;
using TownSystem.FireService.Modules;
using TownSystem.FireService.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.FireService.Tests;

public sealed class FireServiceModuleTests
{
    [Fact]
    public void EmergencyReported_Fire_DispatchesAvailableTeam()
    {
        var fixture = CreateFixture();

        Guid emergencyId = Guid.NewGuid();

        fixture.World.Events.Publish(
            CreateEmergency(emergencyId, EmergencyKind.Fire));

        Assert.Equal(
            RescueTeamStatus.Dispatched,
            fixture.Teams[0].Status);

        Assert.All(
            fixture.Teams[0].Members,
            firefighter => Assert.False(firefighter.IsAvailable));
    }

    [Fact]
    public void EmergencyReported_SameEmergencyTwice_DoesNotDispatchSecondTeam()
    {
        var fixture = CreateFixture(teamCount: 2);

        Guid emergencyId = Guid.NewGuid();

        var emergency = CreateEmergency(
            emergencyId,
            EmergencyKind.Fire);

        fixture.World.Events.Publish(emergency);
        fixture.World.Events.Publish(emergency);

        Assert.Equal(
            RescueTeamStatus.Dispatched,
            fixture.Teams[0].Status);

        Assert.Equal(
            RescueTeamStatus.Available,
            fixture.Teams[1].Status);
    }

    [Fact]
    public void EmergencyResolved_ReleasesDispatchedTeam()
    {
        var fixture = CreateFixture();

        Guid emergencyId = Guid.NewGuid();

        fixture.World.Events.Publish(
            CreateEmergency(emergencyId, EmergencyKind.Fire));

        fixture.World.Events.Publish(
            new EmergencyResolved(emergencyId));

        Assert.Equal(
            RescueTeamStatus.Available,
            fixture.Teams[0].Status);

        Assert.All(
            fixture.Teams[0].Members,
            firefighter => Assert.True(firefighter.IsAvailable));
    }

    [Fact]
    public void EmergencyCancelled_ReleasesDispatchedTeam()
    {
        var fixture = CreateFixture();

        Guid emergencyId = Guid.NewGuid();

        fixture.World.Events.Publish(
            CreateEmergency(emergencyId, EmergencyKind.Fire));

        fixture.World.Events.Publish(
            new EmergencyCancelled(emergencyId));

        Assert.Equal(
            RescueTeamStatus.Available,
            fixture.Teams[0].Status);

        Assert.All(
            fixture.Teams[0].Members,
            firefighter => Assert.True(firefighter.IsAvailable));
    }

    [Fact]
    public void EmergencyReported_NonFire_DoesNotDispatchTeam()
    {
        var fixture = CreateFixture();

        fixture.World.Events.Publish(
            CreateEmergency(Guid.NewGuid(), EmergencyKind.Accident));

        Assert.Equal(
            RescueTeamStatus.Available,
            fixture.Teams[0].Status);

        Assert.All(
            fixture.Teams[0].Members,
            firefighter => Assert.True(firefighter.IsAvailable));
    }

    private static EmergencyReported CreateEmergency(
        Guid id,
        EmergencyKind kind)
    {
        return new EmergencyReported(
            id,
            kind,
            new Position(10, 10),
            3);
    }

    private static TestFixture CreateFixture(int teamCount = 1)
    {
        var world = new World();
        var registry = new FakeBuildingRegistry();

        // Регистрируем общий реестр до запуска модулей.
        world.RegisterService<IBuildingRegistry>(registry);

        var module = new FireServiceModule();
        world.RegisterModule(module);

        // Подключаем зависимости в OnWorldReady.
        world.Start();

        var service =
            world.GetService<IFireServiceManagementService>();

        var station = new FireStation(
            "Тестовая пожарная станция",
            new Position(5, 5),
            new Footprint(2, 2),
            capacity: 20);

        Assert.True(service.TryRegisterStation(station));

        var teams = new List<RescueTeam>();

        for (int i = 0; i < teamCount; i++)
        {
            var firefighter = new Firefighter(
                $"Пожарный {i + 1}");

            var team = new RescueTeam(
                $"Команда {i + 1}",
                new[] { firefighter });

            Assert.True(
                service.TryAssignTeam(station.Id, team));

            teams.Add(team);
        }

        return new TestFixture(world, teams);
    }

    private sealed record TestFixture(
        World World,
        List<RescueTeam> Teams);

    /// <summary>
    /// Тестовая реализация общего реестра зданий.
    /// </summary>
    private sealed class FakeBuildingRegistry : IBuildingRegistry
    {
        private readonly Dictionary<EntityId, IBuilding> _buildings = new();

        public IReadOnlyCollection<IBuilding> All =>
            _buildings.Values.ToArray();

        public bool TryAdd(IBuilding building)
        {
            ArgumentNullException.ThrowIfNull(building);

            if (_buildings.ContainsKey(building.Id))
                return false;

            _buildings.Add(building.Id, building);
            return true;
        }

        public bool Remove(EntityId id)
        {
            return _buildings.Remove(id);
        }

        public IBuilding? Find(EntityId id)
        {
            return _buildings.GetValueOrDefault(id);
        }

        public IEnumerable<IBuilding> FindNear(
            Position center,
            double radius)
        {
            return _buildings.Values.Where(
                building =>
                    building.Position.DistanceTo(center) <= radius);
        }
    }
}
