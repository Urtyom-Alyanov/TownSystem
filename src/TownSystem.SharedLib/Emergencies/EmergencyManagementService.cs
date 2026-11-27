using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.SharedLib.Emergencies;

/// <summary>
/// Сервис управления чрезвычайными происшествиями.
/// </summary>
public sealed class EmergencyManagementService
    : IEmergencyManagementService
{
    private readonly Dictionary<EntityId, EmergencyIncident> _incidents = new();

    private readonly IEventBus _eventBus;

    private readonly object _sync = new();

    public EmergencyManagementService(IEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        _eventBus = eventBus;
    }
    public IReadOnlyCollection<EmergencyIncident> Incidents
    {
        get
        {
            lock (_sync)
            {
                return _incidents.Values.ToArray();
            }
        }
    }
    public EmergencyIncident Report(
        string name,
        EmergencyKind kind,
        Position location,
        int severity,
        EntityId? subjectId = null)
    {
        var incident = new EmergencyIncident(
            name,
            kind,
            location,
            severity,
            subjectId);

        lock (_sync)
        {
            _incidents.Add(incident.Id, incident);
        }

        _eventBus.Publish(
            new EmergencyReported(
                incident.Id.Value,
                incident.Kind,
                incident.Location,
                incident.Severity,
                incident.SubjectId));

        return incident;
    }
    public EmergencyIncident? FindById(EntityId id)
    {
        lock (_sync)
        {
            return _incidents.TryGetValue(id, out var incident)
                ? incident
                : null;
        }
    }
    public bool StartResponse(EntityId id)
    {
        lock (_sync)
        {
            if (!_incidents.TryGetValue(id, out var incident))
                return false;

            if (incident.Status != EmergencyStatus.Reported)
                return false;

            incident.StartResponse();

            return true;
        }
    }
    public bool Resolve(EntityId id)
    {
        lock (_sync)
        {
            if (!_incidents.TryGetValue(id, out var incident))
                return false;

            if (incident.Status != EmergencyStatus.InProgress)
                return false;

            incident.MarkResolved();
        }

        _eventBus.Publish(
            new EmergencyResolved(id.Value));

        return true;
    }
    public bool Cancel(EntityId id)
    {
        lock (_sync)
        {
            if (!_incidents.TryGetValue(id, out var incident))
                return false;

            if (incident.Status is not (
                EmergencyStatus.Reported or
                EmergencyStatus.InProgress))
            {
                return false;
            }

            incident.Cancel();
        }

        _eventBus.Publish(
            new EmergencyCancelled(id.Value));

        return true;
    }

    /// <summary>
    /// Восстанавливает список происшествий без публикации событий.
    /// </summary>
    internal void RestoreIncidents(
        IEnumerable<EmergencyIncident> incidents)
    {
        ArgumentNullException.ThrowIfNull(incidents);

        var restored = incidents.ToArray();
        var dictionary = new Dictionary<EntityId, EmergencyIncident>();

        foreach (var incident in restored)
        {
            if (!dictionary.TryAdd(incident.Id, incident))
                throw new InvalidDataException(
                    $"Повторяющийся идентификатор происшествия: {incident.Id}");
        }

        lock (_sync)
        {
            _incidents.Clear();

            foreach (var pair in dictionary)
                _incidents.Add(pair.Key, pair.Value);
        }
    }
}