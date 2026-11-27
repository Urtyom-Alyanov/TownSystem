using TownSystem.SharedLib.Core;

namespace TownSystem.FireService.Models;

public enum RescueTeamStatus
{
    Available,
    Dispatched
}

/// <summary>Пожарно-спасательная команда из сотрудников службы.</summary>
public sealed class RescueTeam : EntityBase
{
    private readonly List<Firefighter> _members;

    public RescueTeam(
        string name,
        IEnumerable<Firefighter> members,
        EntityId? id = null)
        : base(name, id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("У команды должно быть название.", nameof(name));

        ArgumentNullException.ThrowIfNull(members);

        _members = members.ToList();

        if (_members.Count == 0)
            throw new ArgumentException(
                "В команде должен быть хотя бы один сотрудник.",
                nameof(members));

        if (_members.Any(member => member is null))
            throw new ArgumentException(
                "Состав команды не может содержать пустых сотрудников.",
                nameof(members));

        if (_members.Select(member => member.Id).Distinct().Count() != _members.Count)
            throw new ArgumentException(
                "Один сотрудник не может быть добавлен в команду несколько раз.",
                nameof(members));
    }

    public IReadOnlyList<Firefighter> Members => _members.AsReadOnly();

    public EntityId? StationId { get; private set; }

    public RescueTeamStatus Status { get; private set; } =
        RescueTeamStatus.Available;

    public bool IsAvailable =>
        Status == RescueTeamStatus.Available &&
        _members.All(member => member.IsAvailable);

    public int AvailableMemberCount =>
        _members.Count(member => member.IsAvailable);

    /// <summary>
    /// Отвязывает команду от пожарной станции.
    /// </summary>
    internal bool TryUnassignFromStation()
    {
        if (Status != RescueTeamStatus.Available)
            return false;

        StationId = null;
        return true;
    }

    /// <summary>
    /// Привязываем команду к пожарной станции.
    /// </summary>
    internal void AssignToStation(EntityId stationId)
    {
        if (StationId is not null && StationId != stationId)
            throw new InvalidOperationException(
                "Команда уже закреплена за другой станцией.");

        StationId = stationId;
    }

    internal bool TryDispatch()
    {
        if (!IsAvailable)
            return false;

        Status = RescueTeamStatus.Dispatched;

        foreach (Firefighter member in _members)
            member.SetDispatched();

        return true;
    }

    internal void Release()
    {
        Status = RescueTeamStatus.Available;

        foreach (Firefighter member in _members)
            member.SetAvailable();
    }
    /// <summary>
    /// Добавляет сотрудника в команду.
    /// </summary>
    internal bool TryAddMember(Firefighter firefighter)
    {
        ArgumentNullException.ThrowIfNull(firefighter);

        if (Status != RescueTeamStatus.Available)
            return false;

        if (_members.Any(member => member.Id == firefighter.Id))
            return false;

        _members.Add(firefighter);
        return true;
    }

    /// <summary>
    /// Удаляет сотрудника из команды.
    /// </summary>
    internal bool TryRemoveMember(EntityId firefighterId)
    {
        if (Status != RescueTeamStatus.Available)
            return false;

        // В команде должен оставаться хотя бы один сотрудник.
        if (_members.Count <= 1)
            return false;

        int index = _members.FindIndex(
            member => member.Id == firefighterId);

        if (index < 0)
            return false;

        _members.RemoveAt(index);
        return true;
    }
}