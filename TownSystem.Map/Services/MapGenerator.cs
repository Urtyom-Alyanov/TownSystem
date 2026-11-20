using System;
using System.Collections.Generic;
using System.Text;

using TownSystem.Map.Models;
using TownSystem.Map.Terrains;

namespace TownSystem.Map.Services;

/// <summary>
/// Схема работы генератора: проходит по всем клеткам карты и последовательно определяет их хар-ки.
/// Сначала координаты, потом высота, потом местность, потом генерация ресурса, потом создание клетки карты и ее добавление на карту
/// </summary>
public class MapGenerator
{
    private readonly Random random = new Random();

    public Models.Map Generate(int width, int height)
    {
        Models.Map map = new Models.Map(width, height);

        // Генерация местности
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int elevation = GenerateHeight(x, y);

                Terrain terrain = GenerateTerrain(elevation);

                ResourceDeposit? deposit = GenerateResource(terrain);

                MapCell cell = new MapCell(
                    new Coordinates(x, y),
                    terrain,
                    new Relief(elevation),
                    deposit);

                map.SetCell(cell);
            }
        }

        return map;
    }

    // Генерация высоты участка
    private int GenerateHeight(int x, int y)
    {
        double value = Math.Sin(x * 0.15) +
                       Math.Cos(y * 0.15) +
                       Math.Sin((x + y) * 0.1);

        int height = (int)(150 + value * 100);

        return height;
    }

    // Определение типа местности по высоте
    private Terrain GenerateTerrain(int height)
    {
        if (height < 0)
            return new WaterBody();

        if (height < 180)
            return new Plain();

        if (height < 300)
            return new Forest();

        return new Mountain();
    }

    // Генерация природных ресурсов
    private ResourceDeposit? GenerateResource(Terrain terrain)
    {
        if (terrain is Forest)
        {
            return new ResourceDeposit(NaturalResource.Wood, random.Next(500, 2001));
        }

        if (terrain is Mountain && random.Next(100) < 30)
        {
            NaturalResource[] resources =
            {
                NaturalResource.Iron,
                NaturalResource.Copper,
                NaturalResource.Coal
            };

            return new ResourceDeposit(resources[random.Next(resources.Length)], random.Next(1000, 5001));
        }

        if (terrain is Plain && random.Next(100) < 5)
        {
            NaturalResource resource = random.Next(2) == 0 ? NaturalResource.Oil : NaturalResource.Gas;

            return new ResourceDeposit(resource, random.Next(1000, 5001));
        }

        return null;
    }
}
