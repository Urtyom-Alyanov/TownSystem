namespace TownSystem.SharedLib.Core;

/// <summary>
/// Точка на карте города в клетках.
/// </summary>
/// <param name="X">По горизонтали от левого края</param>
/// <param name="Y">По вертикали от верхнего края</param>
public readonly record struct Position(double X, double Y)
{
    /// <summary>
    /// Расстояние от этой позиции до <paramref name="other"/>
    /// </summary>
    /// <param name="other">Другая позиция</param>
    /// <returns>Расстояние</returns>
    public double DistanceTo(Position other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <inheritdoc />
    public override string ToString() => $"({X:0.#}; {Y:0.#})";
}

/// <summary>
/// Размер объекта на карте в клетках.
/// </summary>
public readonly record struct Footprint(uint Width, uint Height)
{
    /// <summary>
    /// Площадь
    /// </summary>
    public uint Size => Width * Height;
}