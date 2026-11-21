using TownSystem.Map.Terrains;

namespace TownSystem.Map.Models;

/// <summary>
/// одна клетка карты, тут лежат её данные и проверяется что на ней можно поставить
/// </summary>
public class MapCell
{
    public Coordinates Coordinates { get; }

    public Terrain Terrain { get; }

    public Relief Relief { get; }

    public ResourceDeposit? Deposit { get; }

    public bool IsOccupied { get; private set; }

    public bool CanBuild => CanPlace(PlacementKind.Building);

    // Сохраняем данные участка, от координат до ресурса если он есть
    public MapCell(Coordinates coordinates, Terrain terrain, Relief relief, ResourceDeposit? deposit = null)
    {
        Coordinates = coordinates;
        Terrain = terrain;
        Relief = relief;
        Deposit = deposit;
    }

    // смотрим свободна ли клетка и подходит ли она для этого объекта
    public bool CanPlace(PlacementKind kind, NaturalResource? resource = null)
    {
        if (IsOccupied)
            return false;

        // какой нужен тип размещения говорит модуль самого объекта
        switch (kind)
        {
            case PlacementKind.Building:
                return Terrain.CanBuild;
            case PlacementKind.LandInfrastructure:
                return Terrain is not WaterBody;
            case PlacementKind.WaterInfrastructure:
                return Terrain is WaterBody;
            case PlacementKind.Extraction:
                return Terrain is not WaterBody && Deposit != null && !Deposit.IsEmpty &&
                    (resource == null || Deposit.Resource == resource);
            default:
                return false;
        }
    }

    // занимаем клетку по обычным правилам застройки
    public bool Occupy()
    {
        return Occupy(PlacementKind.Building);
    }

    // Занимаем клетку с учётом того какой объект туда ставим
    public bool Occupy(PlacementKind kind, NaturalResource? resource = null)
    {
        if (!CanPlace(kind, resource))
            return false;

        IsOccupied = true;
        return true;
    }

    // очищаем отметку занятости, чтоб клетку можно было использовать снова
    public void Release()
    {
        IsOccupied = false;
    }
}