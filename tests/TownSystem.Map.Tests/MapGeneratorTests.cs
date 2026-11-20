using NUnit.Framework;

using TownSystem.Map.Models;
using TownSystem.Map.Services;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Tests;

/// <summary>
/// Тесты на генерацию карты, тут проверяем размеры, реки, высоты и ресурсы
/// </summary>
public class MapGeneratorTests
{
    // проверяем что выходят именно те размеры, которые передали генератору
    [Test]
    public void GenerateMap()
    {
        MapGenerator generator = new MapGenerator();
        Models.Map map = generator.Generate(50, 50);

        Assert.That(map.Width, Is.EqualTo(50));
        Assert.That(map.Height, Is.EqualTo(50));
    }

    // даже если на карте одна строка или один столбец, генерация должна работать
    [TestCase(1, 1)]
    [TestCase(1, 12)]
    [TestCase(12, 1)]
    public void GenerateNarrowMap(int width, int height)
    {
        Models.Map map = new MapGenerator(42).Generate(width, height);
        MapCell lastCell = map[width - 1, height - 1];

        Assert.That(lastCell.Coordinates.X, Is.EqualTo(width - 1));
        Assert.That(lastCell.Coordinates.Y, Is.EqualTo(height - 1));
    }

    // С одним seed высоты и запасы на двух картах должны совпасть
    [Test]
    public void SameSeedGivesSameMap()
    {
        Models.Map first = new MapGenerator(42).Generate(64, 32);
        Models.Map second = new MapGenerator(42).Generate(64, 32);

        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                MapCell firstCell = first[x, y];
                MapCell secondCell = second[x, y];
                Assert.That(secondCell.Relief.Height, Is.EqualTo(firstCell.Relief.Height));
                Assert.That(secondCell.Deposit?.Resource, Is.EqualTo(firstCell.Deposit?.Resource));
                Assert.That(secondCell.Deposit?.Amount, Is.EqualTo(firstCell.Deposit?.Amount));
            }
        }
    }

    // у реки проверяем что это вода и что в русле нет залежей
    [TestCase(42)]
    [TestCase(87)]
    public void RiversAreWaterWithoutDeposits(int seed)
    {
        Models.Map map = new MapGenerator(seed).Generate(200, 100);
        int riverCells = 0;

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                MapCell cell = map[x, y];
                if (cell.Terrain is not WaterBody { IsRiver: true })
                    continue;

                riverCells++;
                Assert.That(cell.Relief.Height, Is.LessThan(0));
                Assert.That(cell.CanBuild, Is.False);
                Assert.That(cell.Deposit, Is.Null);
            }
        }

        Assert.That(riverCells, Is.GreaterThan(0));
    }

    // смотрим по высоте клетки какая местность должна получиться
    [Test]
    public void CheckTerrain()
    {
        MapGenerator generator = new MapGenerator();
        Models.Map map = generator.Generate(50, 50);

        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                MapCell cell = map[x, y];

                if (cell.Relief.Height < 0)
                    Assert.That(cell.Terrain, Is.TypeOf<WaterBody>());
                else if (cell.Relief.Height < 180)
                    Assert.That(cell.Terrain, Is.TypeOf<Plain>());
                else if (cell.Relief.Height < 300)
                    Assert.That(cell.Terrain, Is.TypeOf<Forest>());
                else
                    Assert.That(cell.Terrain, Is.TypeOf<Mountain>());
            }
        }
    }

    // В лесу есть древесина, в воде месторождений нет
    [Test]
    public void CheckResources()
    {
        MapGenerator generator = new MapGenerator();
        Models.Map map = generator.Generate(50, 50);

        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                MapCell cell = map[x, y];

                if (cell.Terrain is WaterBody)
                    Assert.That(cell.Deposit, Is.Null);

                if (cell.Terrain is Forest)
                {
                    Assert.That(cell.Deposit, Is.Not.Null);
                    Assert.That(cell.Deposit!.Resource,
                        Is.EqualTo(NaturalResource.Wood));
                }
            }
        }
    }

    // проверяем что есть и отдельные залежи, и клетки с таким же ресурсом по соседству
    [TestCase(42)]
    [TestCase(87)]
    public void SpreadMineralsAndFuelAcrossSuitableTerrain(int seed)
    {
        Models.Map map = new MapGenerator(seed).Generate(400, 200);
        int mineralCells = 0;
        int fuelCells = 0;
        int isolatedDeposits = 0;

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                MapCell cell = map[x, y];
                if (cell.Deposit == null || cell.Deposit.Resource == NaturalResource.Wood)
                    continue;

                NaturalResource resource = cell.Deposit.Resource;
                if (resource is NaturalResource.Oil or NaturalResource.Gas)
                {
                    Assert.That(cell.Terrain, Is.TypeOf<Plain>());
                    Assert.That(cell.Relief.Height, Is.LessThanOrEqualTo(135));
                    fuelCells++;
                }
                else
                {
                    Assert.That(cell.Terrain, Is.TypeOf<Mountain>());
                    mineralCells++;
                }

                bool sameToLeft = x > 0 && map[x - 1, y].Deposit?.Resource == resource;
                bool sameToRight = x + 1 < map.Width && map[x + 1, y].Deposit?.Resource == resource;
                bool sameAbove = y > 0 && map[x, y - 1].Deposit?.Resource == resource;
                bool sameBelow = y + 1 < map.Height && map[x, y + 1].Deposit?.Resource == resource;
                if (!sameToLeft && !sameToRight && !sameAbove && !sameBelow)
                    isolatedDeposits++;
            }
        }

        Assert.That(mineralCells, Is.GreaterThan(0));
        Assert.That(fuelCells, Is.GreaterThan(0));
        int totalDeposits = mineralCells + fuelCells;
        Assert.That(isolatedDeposits, Is.GreaterThan(totalDeposits / 3));
        Assert.That(isolatedDeposits, Is.LessThan(totalDeposits));
    }
}
