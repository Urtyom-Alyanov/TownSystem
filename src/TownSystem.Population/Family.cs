using TownSystem.SharedLib.Core;

namespace TownSystem.Population;

/// <summary>
/// Семья - группа людей, живущих вместе.
/// Может содержать супругов, детей и пожилых родственников.
/// </summary>
public class Family
{
    private readonly List<Citizen> members = new();
    private PopulationService? ownerService;

    /// <summary>
    /// Создаёт семью.
    /// </summary>
    /// <param name="id">Уникальный идентификатор семьи</param>
    /// <param name="name">Название семьи (обычно фамилия)</param>
    public Family(EntityId id, string name)
    {
        Id = id;
        Name = name;
    }

    /// <summary>
    /// Уникальный идентификатор семьи.
    /// </summary>
    public EntityId Id { get; }

    /// <summary>
    /// Название семьи (обычно фамилия).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Все члены семьи.
    /// </summary>
    public IReadOnlyCollection<Citizen> Members => members.AsReadOnly();

    /// <summary>
    /// Количество членов семьи.
    /// </summary>
    public int MemberCount => members.Count;

    internal PopulationService? OwnerService => ownerService;

    /// <summary>
    /// Добавляет члена в семью.
    /// Если семья уже зарегистрирована в PopulationService, операция синхронизируется с ним.
    /// </summary>
    /// <param name="citizen">Горожанин</param>
    public void AddMember(Citizen citizen)
    {
        if (citizen == null)
            throw new ArgumentNullException(nameof(citizen));

        if (HasMember(citizen))
            return;

        if (ownerService != null)
        {
            if (!ownerService.AddMemberToFamily(Id, citizen))
                throw new InvalidOperationException("Не удалось добавить гражданина в зарегистрированную семью: гражданин не зарегистрирован в сервисе или уже состоит в другой семье/жилье.");
            return;
        }

        members.Add(citizen);
    }

    /// <summary>
    /// Удаляет члена из семьи (например, при смерти).
    /// Если семья уже зарегистрирована в PopulationService, операция синхронизируется с ним.
    /// </summary>
    /// <param name="citizen">Горожанин</param>
    public void RemoveMember(Citizen citizen)
    {
        if (citizen == null)
            return;

        if (ownerService != null)
        {
            ownerService.RemoveMemberFromFamily(Id, citizen.Id);
            return;
        }

        RemoveMemberInternal(citizen);
    }

    internal void ClearMembersInternal()
    {
        members.Clear();
    }


    /// <summary>
    /// Проверяет, есть ли член в семье.
    /// </summary>
    /// <param name="citizen">Горожанин</param>
    /// <returns>true если член в семье, иначе false</returns>
    public bool HasMember(Citizen citizen)
    {
        if (citizen == null)
            return false;

        return members.Any(m => ReferenceEquals(m, citizen) || m.Id == citizen.Id);
    }

    internal void AttachToService(PopulationService service)
    {
        ownerService = service;
    }

    internal void DetachFromService()
    {
        ownerService = null;
    }

    internal void AddMemberInternal(Citizen citizen)
    {
        if (!HasMember(citizen))
            members.Add(citizen);
    }

    internal bool RemoveMemberInternal(Citizen citizen)
    {
        var index = members.FindIndex(m => ReferenceEquals(m, citizen) || m.Id == citizen.Id);
        if (index < 0)
            return false;

        members.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Расчётный минимальный размер квартиры для семьи.
    /// 1 комната на 2 человека, минимум 1 комната.
    /// </summary>
    public uint MinimalRoomCount => Math.Max(1, (uint)Math.Ceiling(MemberCount / 2.0));

    /// <summary>
    /// Возвращает строковое представление для отладки.
    /// </summary>
    public override string ToString() => $"Family({Name}, members={MemberCount})";
}