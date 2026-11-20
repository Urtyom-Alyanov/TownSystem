using NUnit.Framework;

using TownSystem.Map.Models;
using TownSystem.Map.Services;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Tests;

public class MapGeneratorTests
{
    [Test]
    public void GenerateMap()
    {
        MapGenerator generator = new MapGenerator();
        Models.Map map = generator.Generate(50, 50);

        Assert.That(map.Width, Is.EqualTo(50));
        Assert.That(map.Height, Is.EqualTo(50));
    }

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
}