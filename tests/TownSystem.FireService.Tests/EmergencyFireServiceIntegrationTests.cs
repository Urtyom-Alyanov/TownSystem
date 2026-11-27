using TownSystem.FireService.Models;
using TownSystem.FireService.Modules;
using TownSystem.FireService.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Emergencies;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.FireService.Tests;

public sealed class EmergencyFireServiceIntegrationTests
{
    [Fact]
    public void ReportFire_DispatchesTeam_AndResolveReleasesIt()
    {
        var fixture = CreateFixture();

        // Регистрируем пожар через настоящий сервис ЧС.
        var incident = fixture.EmergencyService.Report(
            "Пожар в жилом доме",
            EmergencyKind.Fire,
            new Position(10, 10),
            3);

        // Пожарная служба должна получить событие
        // и отправить команду.
        Assert.Equal(
            RescueTeamStatus.Dispatched,
            fixture.Team.Status);

        Assert.All(
            fixture.Team.Members,
            firefighter => Assert.False(firefighter.IsAvailable));

        // Переводим происшествие в обработку и завершаем его.
        Assert.True(
            fixture.EmergencyService.StartResponse(incident.Id));

        Assert.True(
            fixture.EmergencyService.Resolve(incident.Id));

        Assert.Equal(
            EmergencyStatus.Resolved,
            incident.Status);

        // После завершения ЧС команда должна освободиться.
        Assert.Equal(
            RescueTeamStatus.Available,
            fixture.Team.Status);

        Assert.All(
            fixture.Team.Members,
            firefighter => Assert.True(firefighter.IsAvailable));
    }

    [Fact]
    public void ReportFire_AndCancel_ReleasesTeam()
    {
        var fixture = CreateFixture();

        var incident = fixture.EmergencyService.Report(
            "Пожар на складе",
            EmergencyKind.Fire,
            new Position(15, 12),
            2);

        Assert.Equal(
            RescueTeamStatus.Dispatched,
            fixture.Team.Status);

        // Отмена допустима и до начала обработки.
        Assert.True(
            fixture.EmergencyService.Cancel(incident.Id));

        Assert.Equal(
            EmergencyStatus.Cancelled,
            incident.Status);

        Assert.Equal(
            RescueTeamStatus.Available,
            fixture.Team.Status);
    }

    [Fact]
    public void ReportAccident_DoesNotDispatchFireTeam()
    {
        var fixture = CreateFixture();

        fixture.EmergencyService.Report(
            "Дорожная авария",
            EmergencyKind.Accident,
            new Position(20, 20),
            3);

        // Пожарная служба не должна реагировать на аварию.
        Assert.Equal(
            RescueTeamStatus.Available,
            fixture.Team.Status);

        Assert.All(
            fixture.Team.Members,
            firefighter => Assert.True(firefighter.IsAvailable));
    }

    private static TestFixture CreateFixture()
    {
        var world = new World();

        // Временно используем заглушку вместо настоящего реестра.
        world.RegisterService<IBuildingRegistry>(
            new FakeBuildingRegistry());

        // Подключаем оба модуля к одному миру.
        world.RegisterModule(new EmergencyModule());
        world.RegisterModule(new FireServiceModule());

        world.Start();

        var emergencyService =
            world.GetService<IEmergencyManagementService>();

        var fireService =
            world.GetService<IFireServiceManagementService>();

        // Создаём пожарную станцию.
        var station = new FireStation(
            "Тестовая пожарная станция",
            new Position(5, 5),
            new Footprint(2, 2),
            capacity: 20);

        Assert.True(
            fireService.TryRegisterStation(station));

        // Создаём и закрепляем пожарную команду.
        var firefighter = new Firefighter("Тестовый пожарный");

        var team = new RescueTeam(
            "Тестовая команда",
            new[] { firefighter });

        Assert.True(
            fireService.TryAssignTeam(station.Id, team));

        return new TestFixture(
            emergencyService,
            fireService,
            team);
    }

    private sealed record TestFixture(
        IEmergencyManagementService EmergencyService,
        IFireServiceManagementService FireService,
        RescueTeam Team);

    /// <summary>
    /// Временная заглушка общего реестра зданий.
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