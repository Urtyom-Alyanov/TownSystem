using TownSystem.SharedLib.Core;

namespace TownSystem.SharedLib.Contracts;

/// <summary>
/// Общая категория здания.
/// По ней модули находят "свои" здания.
/// </summary>
public enum BuildingCategory
{
    Residential, Commercial, Industrial, Utility, Emergency,
    Education, Healthcare, Transport, Recreation, Other
}

/// <summary>
/// Здание на карте. Модули наследуют/оборачивают его в свои типы (школа, цех, участок).
/// </summary>
public interface IBuilding : IEntity
{
    /// <summary>
    /// Категория здания.
    /// </summary>
    BuildingCategory Category { get; }

    /// <summary>
    /// Положение на карте.
    /// </summary>
    Position Position { get; }

    /// <summary>
    /// Размер на карте.
    /// </summary>
    Footprint Footprint { get; }

    /// <summary>
    /// Количество этажей.
    /// </summary>
    uint Floors { get; }

    /// <summary>
    /// Вместимость (жильцов, учеников, пациентов, сотрудников — по смыслу категории).
    /// </summary>
    uint Capacity { get; }

    /// <summary>
    /// Износ от 0 (новое) до 1 (аварийное).
    /// </summary>
    double Wear { get; }
}

/// <summary>
/// Реестр всех зданий города. Регистрируется как <c>IBuildingRegistry</c>.
/// </summary>
public interface IBuildingRegistry
{
    /// <summary>
    /// Все здания.
    /// </summary>
    IReadOnlyCollection<IBuilding> All { get; }

    /// <summary>
    /// Добавляет здание (публикует <c>BuildingAdded</c>).
    /// </summary>
    /// <returns>false, если место занято или не подходит.</returns>
    bool TryAdd(IBuilding building);

    /// <summary>
    /// Сносит здание (публикует <c>BuildingRemoved</c>).
    /// </summary>
    bool Remove(EntityId id);

    /// <summary>
    /// Находит здание по идентификатору.
    /// </summary>
    IBuilding? Find(EntityId id);

    /// <summary>
    /// Здания в радиусе от точки.
    /// </summary>
    IEnumerable<IBuilding> FindNear(Position center, double radius);
}