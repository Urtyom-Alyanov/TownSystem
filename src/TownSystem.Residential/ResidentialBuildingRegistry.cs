using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.Residential;

/// <summary>
/// Реестр всех зданий города (общая реализация IBuildingRegistry).
/// Хранит здания всех категорий, проверяет корректность размещения.
/// </summary>
public sealed class ResidentialBuildingRegistry : IBuildingRegistry
{
    private readonly Dictionary<EntityId, IBuilding> buildings;
    private readonly IEventBus eventBus;
    private IMapService? mapService;

    /// <summary>
    /// Создаёт новый реестр зданий.
    /// </summary>
    /// <param name="eventBus">Шина событий для публикации событий добавления/удаления</param>
    /// <param name="mapService">Сервис карты для проверки допустимости местности</param>
    public ResidentialBuildingRegistry(IEventBus eventBus, IMapService mapService)
        : this(eventBus)
    {
        this.mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
    }

    /// <summary>
    /// Создаёт реестр на этапе Initialize до получения IMapService в OnWorldReady.
    /// </summary>
    internal ResidentialBuildingRegistry(IEventBus eventBus)
    {
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        buildings = new Dictionary<EntityId, IBuilding>();
    }

    /// <summary>
    /// Подключает сервис карты после инициализации всех модулей мира.
    /// </summary>
    internal void SetMapService(IMapService mapService)
    {
        this.mapService = mapService ?? throw new ArgumentNullException(nameof(mapService));
    }

    /// <summary>
    /// Заменяет жилые здания в реестре при загрузке сохранения
    /// без удаления зданий других модулей и без публикации событий.
    /// </summary>
    internal void ReplaceResidentialBuildings(IEnumerable<ResidentialBuilding> loadedBuildings)
    {
        ArgumentNullException.ThrowIfNull(loadedBuildings);

        var residentialIds = buildings
            .Where(kvp => kvp.Value is ResidentialBuilding)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in residentialIds)
        {
            buildings.Remove(id);
        }

        foreach (var building in loadedBuildings)
        {
            if (building == null)
                continue;

            buildings[building.Id] = building;
        }
    }

    /// <summary>
    /// Все зарегистрированные здания.
    /// </summary>
    public IReadOnlyCollection<IBuilding> All => buildings.Values;

    /// <summary>
    /// Добавляет здание в реестр с проверками корректности.
    /// Проверяет:
    /// - Допустимость местности через IMapService
    /// - Пересечения с существующими зданиями
    /// - Выход за границы карты
    /// Публикует событие BuildingAdded при успехе.
    /// </summary>
    /// <param name="building">Здание для добавления</param>
    /// <returns>true, если здание успешно добавлено; false, если место занято или недопустимо</returns>
    public bool TryAdd(IBuilding building)
    {
        if (building == null)
            throw new ArgumentNullException(nameof(building));

        var currentMapService = GetMapService();

        // Проверка, что здание еще не добавлено
        if (buildings.ContainsKey(building.Id))
            return false;

        // Проверка выхода за границы карты
        if (!IsWithinMapBounds(building, currentMapService))
            return false;

        // Проверка пригодности местности
        if (!IsBuildableArea(building, currentMapService))
            return false;

        // Проверка пересечений с другими зданиями
        if (HasOverlap(building))
            return false;

        // Все проверки пройдены
        buildings[building.Id] = building;
        eventBus.Publish(new BuildingAdded(building.Id));
        return true;
    }

    /// <summary>
    /// Удаляет здание из реестра по идентификатору.
    /// Публикует событие BuildingRemoved при успехе.
    /// </summary>
    /// <param name="id">Идентификатор здания</param>
    /// <returns>true, если здание удалено; false, если здание не найдено</returns>
    public bool Remove(EntityId id)
    {
        if (!buildings.Remove(id))
            return false;

        eventBus.Publish(new BuildingRemoved(id));
        return true;
    }

    /// <summary>
    /// Находит здание по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор здания</param>
    /// <returns>Здание или null, если не найдено</returns>
    public IBuilding? Find(EntityId id)
    {
        return buildings.TryGetValue(id, out var building) ? building : null;
    }

    /// <summary>
    /// Находит здания в радиусе от заданной точки.
    /// </summary>
    /// <param name="center">Центральная точка</param>
    /// <param name="radius">Радиус поиска в клетках</param>
    /// <returns>Перечисление зданий в радиусе</returns>
    public IEnumerable<IBuilding> FindNear(Position center, double radius)
    {
        return buildings.Values.Where(b =>
            center.DistanceTo(b.Position) <= radius
        );
    }

    private IMapService GetMapService()
    {
        if (mapService == null)
            throw new InvalidOperationException("Сервис карты (IMapService) ещё не подключён. Убедитесь, что вызван OnWorldReady().");

        return mapService;
    }

    /// <summary>
    /// Проверяет, находится ли здание полностью внутри границ карты.
    /// </summary>
    private static bool IsWithinMapBounds(IBuilding building, IMapService currentMapService)
    {
        var minX = (uint)building.Position.X;
        var minY = (uint)building.Position.Y;
        var maxX = minX + building.Footprint.Width - 1;
        var maxY = minY + building.Footprint.Height - 1;

        // Проверяем границы карты для всех углов
        if (!currentMapService.InBounds(minX, minY))
            return false;

        if (!currentMapService.InBounds(maxX, maxY))
            return false;

        return true;
    }

    /// <summary>
    /// Проверяет, что все клетки, занимаемые зданием, пригодны для постройки.
    /// </summary>
    private static bool IsBuildableArea(IBuilding building, IMapService currentMapService)
    {
        var minX = (uint)building.Position.X;
        var minY = (uint)building.Position.Y;
        var maxX = minX + building.Footprint.Width - 1;
        var maxY = minY + building.Footprint.Height - 1;

        for (uint x = minX; x <= maxX; x++)
        {
            for (uint y = minY; y <= maxY; y++)
            {
                if (!currentMapService.IsBuildable(x, y))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Проверяет пересечение здания с существующими зданиями.
    /// </summary>
    private bool HasOverlap(IBuilding building)
    {
        var newMinX = building.Position.X;
        var newMinY = building.Position.Y;
        var newMaxX = newMinX + building.Footprint.Width;
        var newMaxY = newMinY + building.Footprint.Height;

        foreach (var existing in buildings.Values)
        {
            var existMinX = existing.Position.X;
            var existMinY = existing.Position.Y;
            var existMaxX = existMinX + existing.Footprint.Width;
            var existMaxY = existMinY + existing.Footprint.Height;

            // Проверка AABB (Axis-Aligned Bounding Box)
            if (newMinX < existMaxX && newMaxX > existMinX &&
                newMinY < existMaxY && newMaxY > existMinY)
            {
                return true; // Пересечение найдено
            }
        }

        return false;
    }
}