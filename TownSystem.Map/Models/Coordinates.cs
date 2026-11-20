using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Models;

/// <summary>
/// Хранит координаты клетки на карте
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
