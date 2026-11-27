using TownSystem.Map.Models;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Tests;

/// <summary>
/// проверяем размер карты, её клетки и размещение объектов на нескольких участках
/// </summary>
public class MapTests
{
    // Смотрим что ширина и высота сохранились как задали
    [Test]
    public void CreateMap()
    {
        Models.Map map = new Models.Map(50, 30);

        Assert.That(map.Width, Is.EqualTo(50));
        Assert.That(map.Height, Is.EqualTo(30));
    }

    // по координатам должна вернуться та самая клетка
    [Test]
    public void GetCell()
    {
        Models.Map map = new Models.Map(10, 10);

        MapCell cell = map[3, 5];

        Assert.That(cell.Coordinates.X, Is.EqualTo(3));
        Assert.That(cell.Coordinates.Y, Is.EqualTo(5));
    }

    // берём точки внутри карты и снаружи, чтоб проверить границы
    [Test]
    public void CheckMapBoundaries()
    {
        Models.Map map = new Models.Map(10, 10);

        Assert.That(map.IsInside(5, 5), Is.True);
        Assert.That(map.IsInside(-1, 5), Is.False);
        Assert.That(map.IsInside(10, 5), Is.False);
    }

    // Карта с шириной ноль не должна создаваться
    [Test]
    public void InvalidMapSize()
    {
        Assert.Throws<ArgumentException>(() =>
            new Models.Map(0, 10));
    }

    // заменяем клетку и смотрим что поменялись местность и высота
    [Test]
    public void SetCell()
    {
        Models.Map map = new Models.Map(10, 10);

        MapCell cell = new MapCell(
            new Coordinates(2, 3),
            new Mountain(),
            new Relief(350));

        map.SetCell(cell);

        Assert.That(map[2, 3].Terrain, Is.TypeOf<Mountain>());
        Assert.That(map[2, 3].Relief.Height, Is.EqualTo(350));
    }

    // если хоть одна клетка не подходит, остальную часть объекта тоже не ставим
    [Test]
    public void OccupyAreaOnlyWhenEveryCellFits()
    {
        Models.Map map = new Models.Map(3, 2);
        map.SetCell(new MapCell(new Coordinates(2, 1), new WaterBody(), new Relief(-10)));

        Assert.That(map.TryOccupyArea(1, 0, 2, 2, PlacementKind.Building), Is.False);
        Assert.That(map[1, 0].IsOccupied, Is.False);
        Assert.That(map.TryOccupyArea(1, 0, int.MaxValue, 2, PlacementKind.Building), Is.False);
        Assert.That(map.TryOccupyArea(0, 0, 2, 2, PlacementKind.Building), Is.True);
        Assert.That(map[0, 0].IsOccupied, Is.True);
        Assert.That(map[1, 1].IsOccupied, Is.True);
        Assert.That(map[2, 0].IsOccupied, Is.False);
    }
}