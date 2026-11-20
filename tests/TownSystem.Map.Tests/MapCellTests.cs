using NUnit.Framework;

using TownSystem.Map.Models;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Tests;

/// <summary>
/// тут проверяем что можно поставить на клетку, как её занять и потом освободить
/// </summary>
public class MapCellTests
{
    // на свободной равнине можно строить обычное здание
    [Test]
    public void BuildOnPlain()
    {
        MapCell cell = new MapCell(
            new Coordinates(0, 0),
            new Plain(),
            new Relief(100));

        Assert.That(cell.CanBuild, Is.True);
    }

    // В воду обычное здание не поставить
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

    // занятую клетку нельзя занять ещё раз
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

    // после освобождения на клетке можно снова построить
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

    // Смотрим что в горах можно добывать нужный ресурс, если он ещё есть
    [Test]
    public void OccupyMountainForExtraction()
    {
        ResourceDeposit deposit = new ResourceDeposit(NaturalResource.Iron, 100);
        MapCell cell = new MapCell(new Coordinates(0, 0), new Mountain(), new Relief(350), deposit);

        Assert.That(cell.CanBuild, Is.False);
        Assert.That(cell.Occupy(), Is.False);
        Assert.That(cell.CanPlace(PlacementKind.Extraction, NaturalResource.Copper), Is.False);
        Assert.That(cell.Occupy(PlacementKind.Extraction, NaturalResource.Iron), Is.True);
        Assert.That(cell.Occupy(PlacementKind.LandInfrastructure), Is.False);

        cell.Release();
        deposit.Extract(100);
        Assert.That(cell.CanPlace(PlacementKind.Extraction), Is.False);
        Assert.That(cell.CanPlace(PlacementKind.LandInfrastructure), Is.True);
    }

    // водное сооружение ставится на воду, для суши эта проверка не подходит
    [Test]
    public void ReserveWaterForInfrastructure()
    {
        MapCell water = new MapCell(new Coordinates(0, 0), new WaterBody(), new Relief(-15));
        MapCell land = new MapCell(new Coordinates(1, 0), new Plain(), new Relief(0));

        Assert.That(water.Occupy(), Is.False);
        Assert.That(water.CanPlace(PlacementKind.LandInfrastructure), Is.False);
        Assert.That(land.CanPlace(PlacementKind.WaterInfrastructure), Is.False);
        Assert.That(water.Occupy(PlacementKind.WaterInfrastructure), Is.True);
        Assert.That(water.CanPlace(PlacementKind.WaterInfrastructure), Is.False);
    }
}
