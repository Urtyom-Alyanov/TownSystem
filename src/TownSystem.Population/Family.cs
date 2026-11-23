using TownSystem.SharedLib.Core;

namespace TownSystem.Population;

/// <summary>
/// Семья - группа людей, живущих вместе.
/// Может содержать супругов, детей и пожилых родственников.
/// </summary>
public class Family
{
    private readonly List<Citizen> members = new();

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

    /// <summary>
    /// Добавляет члена в семью.
    /// </summary>
    /// <param name="citizen">Горожанин</param>
    public void AddMember(Citizen citizen)
    {
        if (citizen == null)
            throw new ArgumentNullException(nameof(citizen));

        if (!members.Contains(citizen))
            members.Add(citizen);
    }

    /// <summary>
    /// Удаляет члена из семьи (например, при смерти).
    /// </summary>
    /// <param name="citizen">Горожанин</param>
    public void RemoveMember(Citizen citizen)
    {
        members.Remove(citizen);
    }

    /// <summary>
    /// Проверяет, есть ли член в семье.
    /// </summary>
    /// <param name="citizen">Горожанин</param>
    /// <returns>true если член в семье, иначе false</returns>
    public bool HasMember(Citizen citizen)
    {
        return members.Contains(citizen);
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
