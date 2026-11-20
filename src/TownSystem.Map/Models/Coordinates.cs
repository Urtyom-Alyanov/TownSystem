namespace TownSystem.Map.Models;

/// <summary>
/// Координаты клетки
/// </summary>
public class Coordinates
{
    public int X { get; }
    public int Y { get; }

    public Coordinates(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
