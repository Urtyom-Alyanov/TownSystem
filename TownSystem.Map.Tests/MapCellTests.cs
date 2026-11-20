using NUnit.Framework;

using TownSystem.Map.Models;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Tests;

public class MapCellTests
{
    [Test]
    public void BuildOnPlain()
    {
        MapCell cell = new MapCell(
            new Coordinates(0, 0),
            new Plain(),
            new Relief(100));

        Assert.That(cell.CanBuild, Is.True);
    }

    [Test]
    public void BuildOnWater()
    {
        MapCell cell = new MapCell(
            new Coordinates(0, 0),
            new WaterBody(),
            new Relief(-10));

        Assert.That(cell.CanBuild, Is.False);
        Assert.That(cell.Occupy(), Is.False);
    }

    [Test]
    public void OccupyCell()
    {
        MapCell cell = new MapCell(
            new Coordinates(0, 0),
            new Plain(),
            new Relief(100));

        Assert.That(cell.Occupy(), Is.True);
        Assert.That(cell.IsOccupied, Is.True);
        Assert.That(cell.CanBuild, Is.False);
        Assert.That(cell.Occupy(), Is.False);
    }

    [Test]
    public void ReleaseCell()
    {
        MapCell cell = new MapCell(
            new Coordinates(0, 0),
            new Plain(),
            new Relief(100));

        cell.Occupy();
        cell.Release();

        Assert.That(cell.IsOccupied, Is.False);
        Assert.That(cell.CanBuild, Is.True);
    }
}