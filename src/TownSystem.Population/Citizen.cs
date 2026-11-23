using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.Population;

/// <summary>
/// Горожанин - реализация IPerson.
/// </summary>
public class Citizen : IPerson
{
    /// <summary>
    /// Создаёт горожанина.
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="name">Имя</param>
    /// <param name="age">Возраст в годах</param>
    /// <param name="position">Текущая позиция на карте</param>
    public Citizen(EntityId id, string name, uint age, Position position)
    {
        Id = id;
        Name = name;
        Age = age;
        Position = position;
        HomeId = null;
        WorkplaceId = null;
        Education = EducationLevel.None;
        Health = 1.0;
    }

    /// <inheritdoc />
    public EntityId Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public uint Age { get; set; }

    /// <inheritdoc />
    public Position Position { get; set; }

    /// <inheritdoc />
    public EntityId? HomeId { get; internal set; }

    /// <inheritdoc />
    public EntityId? WorkplaceId { get; set; }

    /// <inheritdoc />
    public EducationLevel Education { get; set; }

    /// <inheritdoc />
    public double Health { get; set; }

    /// <summary>
    /// Возвращает строковое представление для отладки.
    /// </summary>
    public override string ToString() => $"Citizen({Name}, age={Age}, health={Health:0.00})";
}
