using TownSystem.SharedLib.Core;

namespace TownSystem.SharedLib.Contracts;

/// <summary>
/// Способ передвижения.
/// </summary>
public enum TransportMode { Pedestrian, Bicycle, Car, Bus, Rail, Ship, Air }

/// <summary>
/// Маршрут.
/// Последовательность точек и общая длина.
/// </summary>
public sealed record Route(IReadOnlyList<Position> Points, double Length);

/// <summary>
/// Дорожная сеть и построение маршрутов. Регистрируется как <c>IRoadNetwork</c>.
/// </summary>
public interface IRoadNetwork
{
    /// <summary>
    /// Строит маршрут между точками для способа передвижения или возвращает null, если пути нет.
    /// </summary>
    Route? FindRoute(Position from, Position to, TransportMode mode);
}