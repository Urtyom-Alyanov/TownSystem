using System;
using System.Collections.Generic;
using System.Text;

using TownSystem.Map.Terrains;

namespace TownSystem.Map.Models;

/// <summary>
/// Отдельная клетка карты, объединяющая все сущности
/// </summary>
public class MapCell
{
    public Coordinates Coordinates { get; }

    public Terrain Terrain { get; }

    public Relief Relief { get; }

    public ResourceDeposit? Deposit { get; }

    public bool IsOccupied { get; private set; }

    public bool CanBuild => Terrain.CanBuild && !IsOccupied;

    public MapCell(Coordinates coordinates, Terrain terrain,
                    Relief relief, ResourceDeposit? deposit = null)
    {
        Coordinates = coordinates;
        Terrain = terrain;
        Relief = relief;
        Deposit = deposit;
    }

    // Занятие клетки объектом
    public bool Occupy()
    {
        if (!CanBuild)
            return false;

        IsOccupied = true;

        return true;
    }

    // Освобождение клетки
    public void Release()
    {
        IsOccupied = false;
    }
}

