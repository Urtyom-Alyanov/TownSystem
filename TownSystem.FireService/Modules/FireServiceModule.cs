using System.Text.Json.Nodes;

using TownSystem.FireService.Models;
using TownSystem.FireService.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.FireService.Modules;

public sealed class FireServiceModule : ISimulationModule, ISaveable
{
    private IWorld? _world;
    private FireServiceManagementService? _service;
    private IDisposable? _buildingRemovedSubscription;
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

    public string SaveKey => "fire-service";

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

        _buildingRemovedSubscription =
            world.Events.Subscribe<BuildingRemoved>(
                HandleBuildingRemoved);
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

    /// <summary>
    /// Сохраняет станции, команды и текущее распределение пожарных вызовов.
    /// </summary>
    public JsonNode Save()
    {
        if (_service is null)
            throw new InvalidOperationException(
                "Пожарная служба не была инициализирована.");

        var stations = new JsonArray();

        foreach (FireStation station in _service.Stations)
        {
            stations.Add(new JsonObject
            {
                ["id"] = station.Id.Value.ToString("D"),
                ["name"] = station.Name,
                ["x"] = station.Position.X,
                ["y"] = station.Position.Y,
                ["width"] = station.Footprint.Width,
                ["height"] = station.Footprint.Height,
                ["capacity"] = station.Capacity,
                ["floors"] = station.Floors,
                ["wear"] = station.Wear
            });
        }

        var teams = new JsonArray();

        foreach (RescueTeam team in _service.Teams)
        {
            var members = new JsonArray();

            foreach (Firefighter member in team.Members)
            {
                members.Add(new JsonObject
                {
                    ["id"] = member.Id.Value.ToString("D"),
                    ["name"] = member.Name,
                    ["rank"] = member.Rank.ToString(),
                    ["experienceYears"] = member.ExperienceYears,
                    ["isOnDuty"] = member.IsOnDuty
                });
            }

            teams.Add(new JsonObject
            {
                ["id"] = team.Id.Value.ToString("D"),
                ["name"] = team.Name,
                ["stationId"] = team.StationId?.Value.ToString("D"),
                ["status"] = team.Status.ToString(),
                ["members"] = members
            });
        }

        var activeAssignments = new JsonArray();

        foreach (var assignment in _emergencyTeams.OrderBy(
                     item => item.Key))
        {
            activeAssignments.Add(new JsonObject
            {
                ["emergencyId"] = assignment.Key.ToString("D"),
                ["teamId"] = assignment.Value.Value.ToString("D")
            });
        }

        var pendingEmergencies = new JsonArray();

        foreach (var item in _pendingEmergencies.UnorderedItems
                     .Where(item =>
                         _pendingEmergencyIds.Contains(
                             item.Element.EmergencyId))
                     .OrderBy(item => item.Priority.Severity)
                     .ThenBy(item => item.Priority.Order))
        {
            EmergencyReported emergency = item.Element;

            pendingEmergencies.Add(new JsonObject
            {
                ["emergencyId"] = emergency.EmergencyId.ToString("D"),
                ["kind"] = emergency.Kind.ToString(),
                ["x"] = emergency.Location.X,
                ["y"] = emergency.Location.Y,
                ["severity"] = emergency.Severity,
                ["subjectId"] = emergency.SubjectId?.Value.ToString("D"),
                ["order"] = item.Priority.Order
            });
        }

        return new JsonObject
        {
            ["stations"] = stations,
            ["teams"] = teams,
            ["activeAssignments"] = activeAssignments,
            ["pendingEmergencies"] = pendingEmergencies,
            ["nextEmergencyOrder"] = _nextEmergencyOrder
        };
    }

    /// <summary>
    /// Восстанавливает станции, команды и распределение вызовов из сохранения.
    /// </summary>
    public void Load(JsonNode state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (_service is null)
            throw new InvalidOperationException(
                "Пожарная служба не была инициализирована.");

        var stations = new List<FireStation>();

        foreach (JsonNode? node in ReadArray(state, "stations"))
        {
            JsonObject item = node?.AsObject()
                ?? throw new InvalidDataException(
                    "Некорректная запись пожарной станции.");

            var station = new FireStation(
                ReadString(item, "name"),
                new Position(
                    ReadDouble(item, "x"),
                    ReadDouble(item, "y")),
                new Footprint(
                    ReadUInt(item, "width"),
                    ReadUInt(item, "height")),
                ReadUInt(item, "capacity"),
                ReadUInt(item, "floors"),
                ReadDouble(item, "wear"),
                ReadEntityId(item, "id"));

            stations.Add(station);
        }

        var teamStates = new List<(
            RescueTeam Team,
            EntityId? StationId,
            bool IsDispatched)>();

        var teamIds = new HashSet<EntityId>();
        var dispatchedTeamIds = new HashSet<EntityId>();

        foreach (JsonNode? node in ReadArray(state, "teams"))
        {
            JsonObject item = node?.AsObject()
                ?? throw new InvalidDataException(
                    "Некорректная запись пожарной команды.");

            var members = new List<Firefighter>();

            foreach (JsonNode? memberNode in ReadArray(item, "members"))
            {
                JsonObject memberItem = memberNode?.AsObject()
                    ?? throw new InvalidDataException(
                        "Некорректная запись пожарного.");

                if (!Enum.TryParse(
                        ReadString(memberItem, "rank"),
                        out FirefighterRank rank) ||
                    !Enum.IsDefined(rank))
                {
                    throw new InvalidDataException(
                        "Некорректное звание пожарного.");
                }

                members.Add(new Firefighter(
                    ReadString(memberItem, "name"),
                    rank,
                    ReadInt(memberItem, "experienceYears"),
                    ReadBool(memberItem, "isOnDuty"),
                    ReadEntityId(memberItem, "id")));
            }

            if (!Enum.TryParse(
                    ReadString(item, "status"),
                    out RescueTeamStatus status) ||
                !Enum.IsDefined(status))
            {
                throw new InvalidDataException(
                    "Некорректный статус пожарной команды.");
            }

            var team = new RescueTeam(
                ReadString(item, "name"),
                members,
                ReadEntityId(item, "id"));

            if (!teamIds.Add(team.Id))
                throw new InvalidDataException(
                    $"Повторяющийся идентификатор команды: {team.Id}.");

            bool isDispatched = status == RescueTeamStatus.Dispatched;
            if (isDispatched)
                dispatchedTeamIds.Add(team.Id);

            teamStates.Add((
                team,
                ReadOptionalEntityId(item, "stationId"),
                isDispatched));
        }

        var activeAssignments = new Dictionary<Guid, EntityId>();
        var assignedTeamIds = new HashSet<EntityId>();

        foreach (JsonNode? node in ReadArray(state, "activeAssignments"))
        {
            JsonObject item = node?.AsObject()
                ?? throw new InvalidDataException(
                    "Некорректная запись активного вызова.");

            Guid emergencyId = Guid.Parse(
                ReadString(item, "emergencyId"));
            EntityId teamId = ReadEntityId(item, "teamId");

            if (!dispatchedTeamIds.Contains(teamId))
                throw new InvalidDataException(
                    "Активный вызов назначен команде, которая не находится на выезде.");

            if (!assignedTeamIds.Add(teamId) ||
                !activeAssignments.TryAdd(emergencyId, teamId))
            {
                throw new InvalidDataException(
                    "Повторяется команда или происшествие в активных вызовах.");
            }
        }

        // Каждая выехавшая команда должна иметь ровно один активный вызов,
        // и каждый активный вызов должен ссылаться на выехавшую команду.
        if (!dispatchedTeamIds.SetEquals(assignedTeamIds))
        {
            throw new InvalidDataException(
                "Список выехавших команд не соответствует активным назначениям.");
        }

        var pendingItems = new List<(EmergencyReported Emergency, long Order)>();
        var pendingIds = new HashSet<Guid>();
        var pendingOrders = new HashSet<long>();

        foreach (JsonNode? node in ReadArray(state, "pendingEmergencies"))
        {
            JsonObject item = node?.AsObject()
                ?? throw new InvalidDataException(
                    "Некорректная запись ожидающего вызова.");

            Guid emergencyId = Guid.Parse(
                ReadString(item, "emergencyId"));

            if (!pendingIds.Add(emergencyId) ||
                activeAssignments.ContainsKey(emergencyId))
            {
                throw new InvalidDataException(
                    "Повторяется идентификатор ожидающего или активного вызова.");
            }

            if (!Enum.TryParse(
                    ReadString(item, "kind"),
                    out EmergencyKind kind) ||
                !Enum.IsDefined(kind) ||
                kind != EmergencyKind.Fire)
            {
                throw new InvalidDataException(
                    "В очереди пожарной службы найден вызов другого типа.");
            }

            int severity = ReadInt(item, "severity");
            if (severity is < 1 or > 5)
                throw new InvalidDataException(
                    "Некорректная тяжесть ожидающего вызова.");

            long order = ReadLong(item, "order");
            if (order < 0 || !pendingOrders.Add(order))
                throw new InvalidDataException(
                    "Некорректный порядок ожидающих вызовов.");

            EntityId? subjectId =
                ReadOptionalEntityId(item, "subjectId");

            var emergency = new EmergencyReported(
                emergencyId,
                kind,
                new Position(
                    ReadDouble(item, "x"),
                    ReadDouble(item, "y")),
                severity,
                subjectId);

            pendingItems.Add((emergency, order));
        }

        long nextEmergencyOrder =
            ReadLong(state.AsObject(), "nextEmergencyOrder");

        long minimumNextOrder = pendingItems.Count == 0
            ? 0
            : pendingItems.Max(item => item.Order) + 1;

        if (nextEmergencyOrder < minimumNextOrder)
            throw new InvalidDataException(
                "Счётчик очереди меньше сохранённого порядка вызовов.");

        _service.RestoreState(stations, teamStates);

        _emergencyTeams.Clear();
        foreach (var assignment in activeAssignments)
            _emergencyTeams.Add(assignment.Key, assignment.Value);

        _pendingEmergencies.Clear();
        _pendingEmergencyIds.Clear();

        foreach (var item in pendingItems)
        {
            _pendingEmergencies.Enqueue(
                item.Emergency,
                (-item.Emergency.Severity, item.Order));
            _pendingEmergencyIds.Add(item.Emergency.EmergencyId);
        }

        _nextEmergencyOrder = nextEmergencyOrder;
    }

    private void HandleBuildingRemoved(BuildingRemoved building)
    {
        _service?.RemoveStation(building.BuildingId);
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

            RescueTeam? selectedTeam = null;

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
            _service.ReleaseTeam(teamId);

        // Удаляем ожидающий вызов, если команда ещё не была назначена.
        _pendingEmergencyIds.Remove(emergencyId);

        // Освободившаяся команда может принять следующий вызов.
        DispatchPendingEmergencies();
    }

    private static JsonArray ReadArray(JsonNode item, string property)
    {
        return item[property]?.AsArray()
            ?? throw new InvalidDataException(
                $"В сохранении отсутствует список {property}.");
    }

    private static string ReadString(JsonObject item, string property)
    {
        return item[property]?.GetValue<string>()
            ?? throw new InvalidDataException(
                $"В сохранении отсутствует поле {property}.");
    }

    private static int ReadInt(JsonObject item, string property)
    {
        return item[property]?.GetValue<int>()
            ?? throw new InvalidDataException(
                $"В сохранении отсутствует поле {property}.");
    }

    private static uint ReadUInt(JsonObject item, string property)
    {
        return item[property]?.GetValue<uint>()
            ?? throw new InvalidDataException(
                $"В сохранении отсутствует поле {property}.");
    }

    private static long ReadLong(JsonObject item, string property)
    {
        return item[property]?.GetValue<long>()
            ?? throw new InvalidDataException(
                $"В сохранении отсутствует поле {property}.");
    }

    private static double ReadDouble(JsonObject item, string property)
    {
        return item[property]?.GetValue<double>()
            ?? throw new InvalidDataException(
                $"В сохранении отсутствует поле {property}.");
    }

    private static bool ReadBool(JsonObject item, string property)
    {
        return item[property]?.GetValue<bool>()
            ?? throw new InvalidDataException(
                $"В сохранении отсутствует поле {property}.");
    }

    private static EntityId ReadEntityId(JsonObject item, string property)
    {
        return new EntityId(Guid.Parse(ReadString(item, property)));
    }

    private static EntityId? ReadOptionalEntityId(
        JsonObject item,
        string property)
    {
        string? value = item[property]?.GetValue<string>();
        return value is null
            ? null
            : new EntityId(Guid.Parse(value));
    }
}
