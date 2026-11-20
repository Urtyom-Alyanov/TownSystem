using TownSystem.SharedLib.Core;

namespace TownSystem.SharedLib.Contracts;

/// <summary>
/// Уровень образования человека.
/// </summary>
public enum EducationLevel { None, School, College, University }

/// <summary>
/// Горожанин
/// </summary>
public interface IPerson : IEntity
{
    /// <summary>
    /// Возраст в годах.
    /// </summary>
    uint Age { get; }

    /// <summary>
    /// Где сейчас находится.
    /// </summary>
    Position Position { get; }

    /// <summary>
    /// Жильё (здание) или null.
    /// </summary>
    EntityId? HomeId { get; }

    /// <summary>
    /// Место работы или учёбы (здание) или null. Назначают модули кадров/образования.
    /// </summary>
    EntityId? WorkplaceId { get; set; }

    /// <summary>
    /// Достигнутое образование. Повышают школы, колледжи и вузы.
    /// </summary>
    EducationLevel Education { get; set; }

    /// <summary>
    /// Здоровье от 0 (при смерти) до 1.
    /// Снижают болезни и происшествия, восстанавливает медицина.
    /// </summary>
    double Health { get; set; }
}

/// <summary>
/// Население города. Регистрируется как <c>IPopulationService</c>.
/// </summary>
public interface IPopulationService
{
    /// <summary>
    /// Все живые люди.
    /// </summary>
    IReadOnlyCollection<IPerson> People { get; }

    /// <summary>
    /// Находит человека.
    /// </summary>
    IPerson? Find(EntityId id);
}