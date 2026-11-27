using System.Globalization;
using System.Text.Json.Nodes;

using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.SharedLib.Emergencies;

/// <summary>
/// Модуль управления чрезвычайными ситуациями.
/// </summary>
public sealed class EmergencyModule : ISimulationModule, ISaveable
{
    private EmergencyManagementService? _service;

    public string Name => "Emergency Management";

    public string SaveKey => "emergencies";

    public void Initialize(IWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _service = new EmergencyManagementService(world.Events);

        world.RegisterService<IEmergencyManagementService>(
            _service);
    }

    public void Tick(TickContext context)
    {
    }

    /// <summary>
    /// Сохраняет все зарегистрированные происшествия.
    /// </summary>
    public JsonNode Save()
    {
        if (_service is null)
            throw new InvalidOperationException(
                "Модуль чрезвычайных ситуаций не инициализирован.");

        var incidents = new JsonArray();

        foreach (var incident in _service.Incidents)
        {
            incidents.Add(new JsonObject
            {
                ["id"] = incident.Id.Value.ToString("D"),
                ["name"] = incident.Name,
                ["kind"] = incident.Kind.ToString(),
                ["x"] = incident.Location.X,
                ["y"] = incident.Location.Y,
                ["severity"] = incident.Severity,
                ["subjectId"] = incident.SubjectId?.Value.ToString("D"),
                ["createdAt"] = incident.CreatedAt.ToString(
                    "O",
                    CultureInfo.InvariantCulture),
                ["status"] = incident.Status.ToString()
            });
        }

        return new JsonObject
        {
            ["incidents"] = incidents
        };
    }

    /// <summary>
    /// Восстанавливает происшествия из сохранения.
    /// </summary>
    public void Load(JsonNode state)
    {
        if (_service is null)
            throw new InvalidOperationException(
                "Модуль чрезвычайных ситуаций не инициализирован.");

        JsonArray incidentsNode =
            state["incidents"]?.AsArray()
            ?? throw new InvalidDataException(
                "В сохранении отсутствует список происшествий.");

        var incidents = new List<EmergencyIncident>();

        foreach (var node in incidentsNode)
        {
            JsonObject item = node?.AsObject()
                ?? throw new InvalidDataException(
                    "Некорректная запись происшествия.");

            Guid id = Guid.Parse(
                ReadString(item, "id"));

            string name = ReadString(item, "name");

            if (!Enum.TryParse(
                    ReadString(item, "kind"),
                    out EmergencyKind kind) ||
                !Enum.IsDefined(kind))
            {
                throw new InvalidDataException(
                    "Некорректный тип происшествия.");
            }

            var location = new Position(
                ReadDouble(item, "x"),
                ReadDouble(item, "y"));

            int severity = ReadInt(item, "severity");

            EntityId? subjectId = null;

            string? subjectIdText =
                item["subjectId"]?.GetValue<string>();

            if (subjectIdText is not null)
                subjectId = new EntityId(Guid.Parse(subjectIdText));

            DateTimeOffset createdAt =
                DateTimeOffset.ParseExact(
                    ReadString(item, "createdAt"),
                    "O",
                    CultureInfo.InvariantCulture);

            if (!Enum.TryParse(
                    ReadString(item, "status"),
                    out EmergencyStatus status) ||
                !Enum.IsDefined(status))
            {
                throw new InvalidDataException(
                    "Некорректный статус происшествия.");
            }

            var incident = new EmergencyIncident(
                name,
                kind,
                location,
                severity,
                subjectId,
                new EntityId(id),
                createdAt);

            // Восстанавливаем жизненный цикл происшествия.
            switch (status)
            {
                case EmergencyStatus.Reported:
                    break;

                case EmergencyStatus.InProgress:
                    incident.StartResponse();
                    break;

                case EmergencyStatus.Resolved:
                    incident.StartResponse();
                    incident.MarkResolved();
                    break;

                case EmergencyStatus.Cancelled:
                    incident.Cancel();
                    break;
            }

            incidents.Add(incident);
        }

        _service.RestoreIncidents(incidents);
    }

    private static string ReadString(
        JsonObject item,
        string property)
    {
        return item[property]?.GetValue<string>()
            ?? throw new InvalidDataException(
                $"Отсутствует поле {property}.");
    }

    private static int ReadInt(
        JsonObject item,
        string property)
    {
        return item[property]?.GetValue<int>()
            ?? throw new InvalidDataException(
                $"Отсутствует поле {property}.");
    }

    private static double ReadDouble(
        JsonObject item,
        string property)
    {
        return item[property]?.GetValue<double>()
            ?? throw new InvalidDataException(
                $"Отсутствует поле {property}.");
    }
}