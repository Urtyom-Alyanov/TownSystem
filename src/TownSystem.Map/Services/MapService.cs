using TownSystem.Map.Models;
using TownSystem.Map.Terrains;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

using MapModel = TownSystem.Map.Models.Map;
using SharedDeposit = TownSystem.SharedLib.Contracts.ResourceDeposit;

namespace TownSystem.Map.Services;

/// <summary>
/// Даёт другим модулям доступ к местности и запасам той же карты
/// </summary>
public sealed class MapService : IMapService
{
    private readonly MapModel map;

    public uint Width => (uint)map.Width;

    public uint Height => (uint)map.Height;

    // Запоминаем готовую карту, чтобы все работали с одними клетками
    public MapService(MapModel map)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
    }

    // Проверяем координаты до перевода из uint в int
    public bool InBounds(uint x, uint y)
    {
        return x < Width && y < Height;
    }

    // Переводим местность клетки в общий тип для других модулей
    public TerrainType GetTerrain(uint x, uint y)
    {
        if (x >= Width)
            throw new ArgumentOutOfRangeException(nameof(x));
        if (y >= Height)
            throw new ArgumentOutOfRangeException(nameof(y));

        Terrain terrain = map[(int)x, (int)y].Terrain;
        return terrain switch
        {
            Plain => TerrainType.Plain,
            Forest => TerrainType.Forest,
            Mountain => TerrainType.Mountain,
            WaterBody => TerrainType.Water,
            _ => throw new InvalidOperationException($"Неизвестный тип местности {terrain.GetType().Name}")
        };
    }

    // Обычная застройка разрешена только на подходящей свободной клетке
    public bool IsBuildable(uint x, uint y)
    {
        return InBounds(x, y) && map[(int)x, (int)y].CanBuild;
    }

    // Возвращаем текущие запасы в формате общей библиотеки
    public SharedDeposit? GetDeposit(uint x, uint y)
    {
        if (!InBounds(x, y))
            return null;

        Models.ResourceDeposit? deposit = map[(int)x, (int)y].Deposit;
        if (deposit == null)
            return null;

        return new SharedDeposit(GetItemType(deposit.Resource), deposit.Amount);
    }

    // Передаём запрос на добычу в месторождение той же карты
    public double Extract(uint x, uint y, double amount)
    {
        if (!InBounds(x, y))
            return 0;

        Models.ResourceDeposit? deposit = map[(int)x, (int)y].Deposit;
        return deposit?.Extract(amount) ?? 0;
    }

    // Подбираем общий код сырья для каждого ресурса карты
    private static ItemType GetItemType(NaturalResource resource)
    {
        return resource switch
        {
            NaturalResource.Iron => ItemType.IronOre,
            NaturalResource.Copper => ItemType.CopperOre,
            NaturalResource.Oil => ItemType.Oil,
            NaturalResource.Gas => ItemType.Gas,
            NaturalResource.Coal => new ItemType("coal"),
            NaturalResource.Wood => ItemType.Wood,
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
    }
}