using TownSystem.Map.Terrains;

namespace TownSystem.Map.Models;

/// <summary>
/// вся карта клеток, проверяем её границы и можно ли занять участок
/// </summary>
public class Map
{
    private readonly MapCell[,] cells;

    public int Width { get; }

    public int Height { get; }

    // делаем обычную карту заданного размера, сначала везде равнина
    public Map(int width, int height) : this(width, height, true)
    {
    }

    // Готовим массив, чтоб генератор мог сразу записать туда свою местность
    internal Map(int width, int height, bool fillWithPlains)
    {
        if (width <= 0 || height <= 0) throw new ArgumentException("Некорректный размер карты");

        Width = width;
        Height = height;

        cells = new MapCell[width, height];

        if (!fillWithPlains)
            return;

        // если это пустая карта, для начала ставим везде равнины
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                cells[x, y] = new MapCell(new Coordinates(x, y), new Plain(), new Relief(0));
            }
        }
    }

    // проверяем что x и y реально попадают на карту
    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    // По x и y достаём нужную клетку
    public MapCell GetCell(int x, int y)
    {
        if (!IsInside(x, y)) throw new ArgumentOutOfRangeException();

        return cells[x, y];
    }

    // если данные участка поменялись, записываем новую клетку на её место
    public void SetCell(MapCell cell)
    {
        int x = cell.Coordinates.X;
        int y = cell.Coordinates.Y;

        if (!IsInside(x, y)) throw new ArgumentOutOfRangeException();

        cells[x, y] = cell;
    }

    // тут проверяем все клетки прямоугольника перед размещением
    public bool CanPlaceArea(int x, int y, int width, int height, PlacementKind kind, NaturalResource? resource = null)
    {
        if (!IsInside(x, y) || width <= 0 || height <= 0 || width > Width - x || height > Height - y)
            return false;

        for (int row = y; row < y + height; row++)
        {
            for (int column = x; column < x + width; column++)
            {
                if (!cells[column, row].CanPlace(kind, resource))
                    return false;
            }
        }

        return true;
    }

    // Занимаем весь участок, когда каждая его клетка подходит
    public bool TryOccupyArea(int x, int y, int width, int height, PlacementKind kind, NaturalResource? resource = null)
    {
        // сначала проверяем все клетки, чтоб не получилось занять только половину
        if (!CanPlaceArea(x, y, width, height, kind, resource))
            return false;

        for (int row = y; row < y + height; row++)
        {
            for (int column = x; column < x + width; column++)
            {
                cells[column, row].Occupy(kind, resource);
            }
        }

        return true;
    }

    public MapCell this[int x, int y]
    {
        get { return GetCell(x, y); }
    }
}