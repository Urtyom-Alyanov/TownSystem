using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Emergencies;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.SharedLib.Tests;

public class EmergencyManagementServiceTests
{
    [Fact]
    public void Report_RegistersIncidentAndPublishesEvent()
    {
        var eventBus = new EventBus();
        var service = new EmergencyManagementService(eventBus);

        var reportedEvents = new List<EmergencyReported>();

        using var subscription =
            eventBus.Subscribe<EmergencyReported>(
                reportedEvents.Add);

        var incident = service.Report(
            "Пожар в жилом доме",
            EmergencyKind.Fire,
            new Position(10, 20),
            3);

        Assert.Single(service.Incidents);
        Assert.Equal(incident, service.FindById(incident.Id));

        var publishedEvent = Assert.Single(reportedEvents);

        Assert.Equal(incident.Id.Value, publishedEvent.EmergencyId);
        Assert.Equal(EmergencyKind.Fire, publishedEvent.Kind);
    }

    [Fact]
    public void StartResponse_ChangesIncidentStatus()
    {
        var service = new EmergencyManagementService(new EventBus());

        var incident = service.Report(
            "Авария",
            EmergencyKind.Accident,
            new Position(5, 5),
            2);

        Assert.True(service.StartResponse(incident.Id));

        Assert.Equal(
            EmergencyStatus.InProgress,
            incident.Status);

        Assert.False(service.StartResponse(incident.Id));
    }

    [Fact]
    public void Resolve_ChangesStatusAndPublishesEvent()
    {
        var eventBus = new EventBus();
        var service = new EmergencyManagementService(eventBus);

        var resolvedEvents = new List<EmergencyResolved>();

        using var subscription =
            eventBus.Subscribe<EmergencyResolved>(
                resolvedEvents.Add);

        var incident = service.Report(
            "Пожар",
            EmergencyKind.Fire,
            new Position(1, 1),
            4);

        service.StartResponse(incident.Id);

        Assert.True(service.Resolve(incident.Id));

        Assert.Equal(
            EmergencyStatus.Resolved,
            incident.Status);

        Assert.Single(resolvedEvents);

        Assert.Equal(
            incident.Id.Value,
            resolvedEvents[0].EmergencyId);

        Assert.False(service.Resolve(incident.Id));
    }

    [Fact]
    public void Cancel_ChangesStatusAndPublishesEvent()
    {
        var eventBus = new EventBus();
        var service = new EmergencyManagementService(eventBus);

        var cancelledEvents = new List<EmergencyCancelled>();

        using var subscription =
            eventBus.Subscribe<EmergencyCancelled>(
                cancelledEvents.Add);

        var incident = service.Report(
            "Поломка инфраструктуры",
            EmergencyKind.Breakdown,
            new Position(7, 9),
            1);

        Assert.True(service.Cancel(incident.Id));

        Assert.Equal(
            EmergencyStatus.Cancelled,
            incident.Status);

        Assert.Single(cancelledEvents);

        Assert.False(service.Cancel(incident.Id));
    }

    [Fact]
    public void FindById_ReturnsNullForUnknownIncident()
    {
        var service = new EmergencyManagementService(new EventBus());

        Assert.Null(service.FindById(EntityId.New()));
    }

    [Fact]
    public void EmergencyModule_RegistersManagementService()
    {
        var world = new World();

        world.RegisterModule(new EmergencyModule());

        var service =
            world.GetService<IEmergencyManagementService>();

        var incident = service.Report(
            "Пожар",
            EmergencyKind.Fire,
            new Position(3, 4),
            2);

        Assert.Single(service.Incidents);

        Assert.Equal(
            EmergencyStatus.Reported,
            incident.Status);
    }
}