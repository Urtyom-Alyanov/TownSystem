using NUnit.Framework;

using TownSystem.Map.Models;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Tests;

public class MapTests
{
    [Test]
    public void CreateMap()
    {
        Models.Map map = new Models.Map(50, 30);

        Assert.That(map.Width, Is.EqualTo(50));
        Assert.That(map.Height, Is.EqualTo(30));
    }

    [Test]
    public void GetCell()
    {
        Models.Map map = new Models.Map(10, 10);

        MapCell cell = map[3, 5];

        Assert.That(cell.Coordinates.X, Is.EqualTo(3));
        Assert.That(cell.Coordinates.Y, Is.EqualTo(5));
    }

    [Test]
    public void CheckMapBoundaries()
    {
        Models.Map map = new Models.Map(10, 10);

        Assert.That(map.IsInside(5, 5), Is.True);
        Assert.That(map.IsInside(-1, 5), Is.False);
        Assert.That(map.IsInside(10, 5), Is.False);
    }

    [Test]
    public void InvalidMapSize()
    {
        Assert.Throws<ArgumentException>(() =>
            new Models.Map(0, 10));
    }

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
}