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

        // Проверка геометрии и выхода за границы карты
        if (!TryGetCellRange(building, currentMapService, out uint minX, out uint minY, out uint maxX, out uint maxY))
            return false;

        if (!currentMapService.InBounds(minX, minY) || !currentMapService.InBounds(maxX, maxY))
            return false;

        // Проверка пригодности местности по всем пересекаемым клеткам
        if (!IsBuildableArea(minX, minY, maxX, maxY, currentMapService))
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
        if (!double.IsFinite(center.X) || !double.IsFinite(center.Y))
            throw new ArgumentOutOfRangeException(nameof(center), "Координаты центра должны быть конечными числами");

        if (double.IsNaN(radius) || radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "Радиус поиска не может быть отрицательным");

        return buildings.Values.Where(b => DistanceToBuilding(center, b) <= radius);
    }

    private IMapService GetMapService()
    {
        if (mapService == null)
            throw new InvalidOperationException("Сервис карты (IMapService) ещё не подключён. Убедитесь, что вызван OnWorldReady().");

        return mapService;
    }

    /// <summary>
    /// Проверяет геометрию здания и вычисляет диапазон клеток карты,
    /// которые пересекает прямоугольник [Position.X, Position.X + Width) × [Position.Y, Position.Y + Height).
    /// </summary>
    private static bool TryGetCellRange(
        IBuilding building,
        IMapService currentMapService,
        out uint minX,
        out uint minY,
        out uint maxX,
        out uint maxY)
    {
        minX = 0;
        minY = 0;
        maxX = 0;
        maxY = 0;

        if (building.Footprint.Width == 0 || building.Footprint.Height == 0)
            return false;

        double startX = building.Position.X;
        double startY = building.Position.Y;

        if (!double.IsFinite(startX) || !double.IsFinite(startY))
            return false;

        if (startX < 0 || startY < 0)
            return false;

        double endX = startX + building.Footprint.Width;
        double endY = startY + building.Footprint.Height;

        if (!double.IsFinite(endX) || !double.IsFinite(endY))
            return false;

        if (endX > currentMapService.Width || endY > currentMapService.Height)
            return false;

        minX = (uint)Math.Floor(startX);
        minY = (uint)Math.Floor(startY);
        maxX = (uint)(Math.Ceiling(endX) - 1.0);
        maxY = (uint)(Math.Ceiling(endY) - 1.0);

        return true;
    }

    /// <summary>
    /// Проверяет, что все клетки в диапазоне пригодны для постройки.
    /// </summary>
    private static bool IsBuildableArea(uint minX, uint minY, uint maxX, uint maxY, IMapService currentMapService)
    {
        for (uint x = minX; ; x++)
        {
            for (uint y = minY; ; y++)
            {
                if (!currentMapService.IsBuildable(x, y))
                    return false;

                if (y == maxY)
                    break;
            }

            if (x == maxX)
                break;
        }

        return true;
    }

    /// <summary>
    /// Вычисляет кратчайшее расстояние от точки до прямоугольника здания на карте.
    /// </summary>
    private static double DistanceToBuilding(Position point, IBuilding building)
    {
        double minX = building.Position.X;
        double minY = building.Position.Y;
        double maxX = minX + building.Footprint.Width;
        double maxY = minY + building.Footprint.Height;

        double dx = Math.Max(0.0, Math.Max(minX - point.X, point.X - maxX));
        double dy = Math.Max(0.0, Math.Max(minY - point.Y, point.Y - maxY));

        return Math.Sqrt(dx * dx + dy * dy);
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