using TownSystem.SharedLib.Core;

namespace TownSystem.SharedLib.Contracts;

/// <summary>
/// Тип местности клетки.
/// </summary>
public enum TerrainType { Plain, Forest, Hills, Mountain, Water, Swamp }

/// <summary>
/// Месторождение в клетке.
/// </summary>
/// <param name="Resource">Вид ресурса</param>
/// <param name="Amount">Количество оставшегося ресурса</param>
public sealed record ResourceDeposit(ItemType Resource, double Amount);

/// <summary>
/// Карта города. Регистрируется в мире как <c>IMapService</c>. Все остальные модули
/// узнают рельеф, воду и месторождения только через этот интерфейс.
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Ширина карты в клетках.
    /// </summary>
    uint Width { get; }

    /// <summary>
    /// Высота карты в клетках.
    /// </summary>
    uint Height { get; }

    /// <summary>
    /// Клетка внутри карты.
    /// </summary>
    bool InBounds(uint x, uint y);

    /// <summary>
    /// Тип местности клетки.
    /// </summary>
    TerrainType GetTerrain(uint x, uint y);

    /// <summary>
    /// Можно ли строить в клетке.
    /// </summary>
    bool IsBuildable(uint x, uint y);

    /// <summary>
    /// Месторождение в клетке или null.
    /// </summary>
    ResourceDeposit? GetDeposit(uint x, uint y);

    /// <summary>
    /// Добывает из месторождения до <paramref name="amount"/>.
    /// </summary>
    /// <returns>Cколько реально извлечено</returns>
    double Extract(uint x, uint y, double amount);
}