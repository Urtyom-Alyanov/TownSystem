using System;
using System.Collections.Generic;
using System.Text;

using TownSystem.Map.Terrains;

namespace TownSystem.Map.Models;

/// <summary>
/// Главный класс, представляющий карту города как двумерный массив
/// </summary>
public class Map
{
    private readonly MapCell[,] cells;

    public int Width { get; }

    public int Height { get; }

    public Map(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Некорректный размер карты");

        Width = width;
        Height = height;

        cells = new MapCell[width, height];

        // Заполнение карты равнинами
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                cells[x, y] = new MapCell(
                    new Coordinates(x, y),
                    new Plain(),
                    new Relief(0));
            }
        }
    }

    // Проверка границ карты
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width &&
               y >= 0 && y < Height;
    }

    // Получение клетки по координатам
    public MapCell GetCell(int x, int y)
    {
        if (!IsInside(x, y))
            throw new ArgumentOutOfRangeException();

        return cells[x, y];
    }

    // Замена клетки
    public void SetCell(MapCell cell)
    {
        int x = cell.Coordinates.X;
        int y = cell.Coordinates.Y;

        if (!IsInside(x, y))
            throw new ArgumentOutOfRangeException();

        cells[x, y] = cell;
    }

    // Индексатор карты
    public MapCell this[int x, int y]
    {
        get { return GetCell(x, y); }
    }
}
