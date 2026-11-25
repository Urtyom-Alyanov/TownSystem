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

    public EntityId Id { get; }

    public string Name { get; }

    public uint Age { get; set; }

    public Position Position { get; set; }

    public EntityId? HomeId { get; internal set; }

    public EntityId? WorkplaceId { get; set; }

    public EducationLevel Education { get; set; }

    private double health = 1.0;

    public double Health
    {
        get => health;
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Здоровье должно быть конечным числом");

            health = Math.Clamp(value, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Возвращает строковое представление для отладки.
    /// </summary>
    public override string ToString() => $"Citizen({Name}, age={Age}, health={Health:0.00})";
}
