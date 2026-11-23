using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Emergencies;
using TownSystem.SharedLib.Events;

namespace TownSystem.SharedLib.Tests;

public class EmergencyIncidentTests
{
    [Fact]
    public void Constructor_CreatesIncident()
    {
        var incident = new EmergencyIncident(
            "Пожар в жилом доме",
            EmergencyKind.Fire,
            new Position(10, 20),
            3);

        Assert.Equal("Пожар в жилом доме", incident.Name);
        Assert.Equal(EmergencyKind.Fire, incident.Kind);
        Assert.Equal(new Position(10, 20), incident.Location);
        Assert.Equal(3, incident.Severity);
        Assert.Equal(EmergencyStatus.Reported, incident.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Constructor_RejectsInvalidSeverity(int severity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmergencyIncident(
                "Пожар",
                EmergencyKind.Fire,
                new Position(1, 1),
                severity));
    }

    [Fact]
    public void Incident_CanBeResolved()
    {
        var incident = new EmergencyIncident(
            "Пожар",
            EmergencyKind.Fire,
            new Position(5, 5),
            2);

        incident.StartResponse();
        incident.MarkResolved();

        Assert.Equal(EmergencyStatus.Resolved, incident.Status);
    }

    [Fact]
    public void Incident_CannotBeResolvedBeforeResponse()
    {
        var incident = new EmergencyIncident(
            "Авария",
            EmergencyKind.Accident,
            new Position(3, 4),
            2);

        Assert.Throws<InvalidOperationException>(
            incident.MarkResolved);
    }

    [Fact]
    public void Incident_CanBeCancelled()
    {
        var incident = new EmergencyIncident(
            "Поломка",
            EmergencyKind.Breakdown,
            new Position(2, 8),
            1);

        incident.Cancel();

        Assert.Equal(EmergencyStatus.Cancelled, incident.Status);
    }
}