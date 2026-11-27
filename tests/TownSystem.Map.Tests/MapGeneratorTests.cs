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
                Assert.That(cell.Relief.Depth, Is.EqualTo(3));
                Assert.That(cell.CanBuild, Is.False);
                Assert.That(cell.Deposit, Is.Null);
            }
        }

        Assert.That(riverCells, Is.GreaterThan(0));
    }

    // Проверяем что у рек есть и лесные, и низкие открытые берега
    [TestCase(42)]
    [TestCase(87)]
    public void RiversHaveForestedAndLowOpenBanks(int seed)
    {
        Models.Map map = new MapGenerator(seed).Generate(1600, 800);
        (int X, int Y)[] steps = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        int forestBanks = 0;
        int lowOpenBanks = 0;

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                MapCell cell = map[x, y];
                if (cell.Terrain is not WaterBody { IsRiver: true })
                    continue;

                foreach (var step in steps)
                {
                    int bankX = x + step.X;
                    int bankY = y + step.Y;
                    if (!map.IsInside(bankX, bankY))
                        continue;

                    MapCell bank = map[bankX, bankY];
                    if (bank.Terrain is WaterBody)
                        continue;

                    int waterLevel = cell.Relief.Height + cell.Relief.Depth;
                    Assert.That(waterLevel, Is.LessThanOrEqualTo(bank.Relief.Height));
                    if (bank.Terrain is Forest)
                        forestBanks++;
                    if (bank.Terrain is Plain && bank.Relief.Height == 0)
                        lowOpenBanks++;
                }
            }
        }

        Assert.That(forestBanks, Is.GreaterThan(0));
        Assert.That(lowOpenBanks, Is.GreaterThan(0));
    }

    // Проверяем что на одной высоте в низине могут быть и лес и открытая равнина
    [TestCase(42)]
    [TestCase(87)]
    public void CheckTerrain(int seed)
    {
        Models.Map map = new MapGenerator(seed).Generate(96, 48);
        HashSet<int> forestHeights = new HashSet<int>();
        HashSet<int> plainHeights = new HashSet<int>();

        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                MapCell cell = map[x, y];
                int height = cell.Relief.Height;

                if (height < 0)
                    Assert.That(cell.Terrain, Is.TypeOf<WaterBody>());
                else if (height >= 300)
                    Assert.That(cell.Terrain, Is.TypeOf<Mountain>());
                else
                    Assert.That(cell.Terrain, Is.TypeOf<Plain>().Or.TypeOf<Forest>());

                if (height >= 0 && height < 100)
                {
                    if (cell.Terrain is Forest)
                        forestHeights.Add(height);
                    if (cell.Terrain is Plain)
                        plainHeights.Add(height);
                }
            }
        }

        Assert.That(forestHeights.Overlaps(plainHeights), Is.True);
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