using TownSystem.Map.Models;
using TownSystem.Map.Services;
using TownSystem.Map.Terrains;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Simulation;

using SharedDeposit = TownSystem.SharedLib.Contracts.ResourceDeposit;

namespace TownSystem.Map.Tests;

/// <summary>
/// Проверяем как другие модули видят карту через общий сервис
/// </summary>
public class MapServiceTests
{
    // Проверяем общий тип местности и зависимость застройки от занятости клетки
    [Test]
    public void TerrainAndBuildingRulesFollowMap()
    {
        Models.Map map = new Models.Map(4, 1);
        map.SetCell(new MapCell(new Coordinates(1, 0), new Forest(), new Relief(30)));
        map.SetCell(new MapCell(new Coordinates(2, 0), new Mountain(), new Relief(350)));
        map.SetCell(new MapCell(new Coordinates(3, 0), new WaterBody(), new Relief(-5)));
        IMapService service = new MapService(map);

        Assert.That(service.Width, Is.EqualTo(4u));
        Assert.That(service.Height, Is.EqualTo(1u));
        Assert.That(service.GetTerrain(0, 0), Is.EqualTo(TerrainType.Plain));
        Assert.That(service.GetTerrain(1, 0), Is.EqualTo(TerrainType.Forest));
        Assert.That(service.GetTerrain(2, 0), Is.EqualTo(TerrainType.Mountain));
        Assert.That(service.GetTerrain(3, 0), Is.EqualTo(TerrainType.Water));
        Assert.That(service.IsBuildable(0, 0), Is.True);
        Assert.That(service.IsBuildable(1, 0), Is.False);
        Assert.That(service.IsBuildable(3, 0), Is.False);

        Assert.That(map[0, 0].Occupy(), Is.True);
        Assert.That(service.IsBuildable(0, 0), Is.False);
    }

    // Проверяем что большие координаты не превращаются в адрес другой клетки
    [Test]
    public void QueriesOutsideMapAreHandled()
    {
        IMapService service = new MapService(new Models.Map(2, 3));

        Assert.That(service.InBounds(1, 2), Is.True);
        Assert.That(service.InBounds(2, 0), Is.False);
        Assert.That(service.InBounds(0, 3), Is.False);
        Assert.That(service.IsBuildable(uint.MaxValue, 0), Is.False);
        Assert.That(service.GetDeposit(uint.MaxValue, 0), Is.Null);
        Assert.That(service.Extract(0, uint.MaxValue, 10), Is.Zero);
        Assert.That(service.GetDeposit(0, 0), Is.Null);
        Assert.That(service.Extract(0, 0, 10), Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetTerrain(uint.MaxValue, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.GetTerrain(0, uint.MaxValue));
    }

    // У железа и меди проверяем именно коды сырья
    [TestCase(NaturalResource.Iron, "iron_ore")]
    [TestCase(NaturalResource.Copper, "copper_ore")]
    [TestCase(NaturalResource.Oil, "oil")]
    [TestCase(NaturalResource.Gas, "gas")]
    [TestCase(NaturalResource.Coal, "coal")]
    [TestCase(NaturalResource.Wood, "wood")]
    public void GetDepositReturnsSharedResourceCode(NaturalResource resource, string code)
    {
        Models.Map map = new Models.Map(1, 1);
        Models.ResourceDeposit deposit = new Models.ResourceDeposit(resource, 8);
        map.SetCell(new MapCell(new Coordinates(0, 0), new Plain(), new Relief(20), deposit));
        IMapService service = new MapService(map);

        SharedDeposit? result = service.GetDeposit(0, 0);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Resource.Code, Is.EqualTo(code));
        Assert.That(result.Amount, Is.EqualTo(8));
    }

    // Добыча на уже занятой шахтой клетке меняет настоящий запас карты
    [Test]
    public void ExtractAcceptsFractionsAndNeverExceedsStock()
    {
        Models.Map map = new Models.Map(1, 1);
        Models.ResourceDeposit deposit = new Models.ResourceDeposit(NaturalResource.Iron, 5);
        map.SetCell(new MapCell(new Coordinates(0, 0), new Mountain(), new Relief(350), deposit));
        IMapService service = new MapService(map);
        Assert.That(map[0, 0].Occupy(PlacementKind.Extraction, NaturalResource.Iron), Is.True);

        Assert.That(service.Extract(0, 0, -1), Is.Zero);
        Assert.That(service.Extract(0, 0, double.NaN), Is.Zero);
        Assert.That(service.Extract(0, 0, double.PositiveInfinity), Is.Zero);
        Assert.That(service.Extract(0, 0, 0.5), Is.EqualTo(0.5));
        Assert.That(service.GetDeposit(0, 0)?.Amount, Is.EqualTo(4.5));
        Assert.That(service.Extract(0, 0, 2.7), Is.EqualTo(2.7));
        Assert.That(deposit.Amount, Is.EqualTo(1.8).Within(0.000001));
        Assert.That(service.Extract(0, 0, double.MaxValue), Is.EqualTo(1.8).Within(0.000001));
        Assert.That(service.GetDeposit(0, 0)?.Amount, Is.Zero);
        Assert.That(service.Extract(0, 0, 1), Is.Zero);
    }

    // Мир получает сервис для той карты, которую передали модулю
    [Test]
    public void ModuleRegistersServiceForExistingMap()
    {
        Models.Map map = new Models.Map(2, 3);
        World world = new World();
        MapModule module = new MapModule(map);
        world.RegisterModule(module);
        world.Start();

        IMapService service = world.GetService<IMapService>();
        Assert.That(module.Order, Is.EqualTo(-100));
        Assert.That(module.Map, Is.SameAs(map));
        Assert.That(service.InBounds(1, 2), Is.True);

        map.SetCell(new MapCell(new Coordinates(1, 2), new Forest(), new Relief(30)));
        Assert.That(service.GetTerrain(1, 2), Is.EqualTo(TerrainType.Forest));
    }
}