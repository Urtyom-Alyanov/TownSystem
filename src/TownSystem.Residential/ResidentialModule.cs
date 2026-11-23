using System.Text.Json;
using System.Text.Json.Nodes;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.Residential;

/// <summary>
/// Модуль управления жилым фондом города.
/// Регистрирует общий реестр зданий и управляет всеми жилыми постройками.
/// </summary>
public sealed class ResidentialModule : ISimulationModule, ISaveable
{
    private ResidentialBuildingRegistry? _registry;
    private IWorld? _world;
    private IMapService? _mapService;

    public string Name => "Residential";
    public string SaveKey => "residential";
    public int Order => -50;

    /// <summary>
    /// Инициализирует модуль: создает реестр и регистрирует его как сервис.
    /// </summary>
    public void Initialize(IWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;

        // Создаем реестр
        _registry = new ResidentialBuildingRegistry(world.Events, world.GetService<IMapService>());

        // Регистрируем реестр как сервис для других модулей
        world.RegisterService<IBuildingRegistry>(_registry);
    }

    /// <summary>
    /// Вызывается после инициализации всех модулей.
    /// Получаем доступ к сервисам других модулей.
    /// </summary>
    public void OnWorldReady()
    {
        if (_world == null)
            throw new InvalidOperationException("Initialize должен быть вызван перед OnWorldReady");

        _mapService = _world.GetService<IMapService>();
    }

    /// <summary>
    /// Выполняет один такт симуляции.
    /// Обновляет износ всех зданий.
    /// </summary>
    public void Tick(TickContext context)
    {
        if (_registry == null)
            return;

        // Обновляем износ всех зданий
        foreach (var building in _registry.All.OfType<ResidentialBuilding>())
        {
            building.AccumulateWear(context.Delta);
        }
    }

    /// <summary>
    /// Сохраняет состояние модуля в JSON.
    /// </summary>
    public JsonNode Save()
    {
        if (_registry == null)
            return new JsonObject();

        var root = new JsonObject();
        var buildingsArray = new JsonArray();

        foreach (var building in _registry.All.OfType<ResidentialBuilding>())
        {
            var buildingObj = new JsonObject
            {
                ["id"] = building.Id.Value.ToString("N"),
                ["name"] = building.Name,
                ["buildingType"] = building.BuildingType.ToString(),
                ["positionX"] = building.Position.X,
                ["positionY"] = building.Position.Y,
                ["footprintWidth"] = building.Footprint.Width,
                ["footprintHeight"] = building.Footprint.Height,
                ["floors"] = building.Floors,
                ["constructionDate"] = building.ConstructionDate.Ticks,
                ["wear"] = building.Wear
            };

            var maintenanceArray = new JsonArray();
            foreach (var record in building.MaintenanceHistory)
            {
                var recordObj = new JsonObject
                {
                    ["date"] = record.Date.Ticks,
                    ["type"] = record.Type.ToString(),
                    ["cost"] = record.Cost,
                    ["wearReduction"] = record.WearReduction,
                    ["description"] = record.Description ?? ""
                };
                maintenanceArray.Add(recordObj);
            }
            buildingObj["maintenanceHistory"] = maintenanceArray;

            buildingsArray.Add(buildingObj);
        }

        root["buildings"] = buildingsArray;
        return root;
    }

    /// <summary>
    /// Восстанавливает состояние модуля из JSON.
    /// </summary>
    public void Load(JsonNode state)
    {
        if (_registry == null || _mapService == null || _world == null)
            return;

        if (state?["buildings"] is not JsonArray buildingsArray)
            return;

        // Очищаем текущий реестр
        var existingIds = _registry.All.Select(b => b.Id).ToList();
        foreach (var id in existingIds)
        {
            _registry.Remove(id);
        }

        // Восстанавливаем здания
        foreach (var buildingNode in buildingsArray.OfType<JsonObject>())
        {
            try
            {
                var id = new EntityId(Guid.ParseExact(buildingNode["id"]!.GetValue<string>(), "N"));
                var name = buildingNode["name"]!.GetValue<string>();
                var buildingType = Enum.Parse<ResidentialBuildingType>(buildingNode["buildingType"]!.GetValue<string>());
                var positionX = buildingNode["positionX"]!.GetValue<double>();
                var positionY = buildingNode["positionY"]!.GetValue<double>();
                var footprintWidth = buildingNode["footprintWidth"]!.GetValue<uint>();
                var footprintHeight = buildingNode["footprintHeight"]!.GetValue<uint>();
                var floors = buildingNode["floors"]!.GetValue<uint>();
                var constructionDate = new DateTime(buildingNode["constructionDate"]!.GetValue<long>());
                var wear = buildingNode["wear"]!.GetValue<double>();

                var building = new ResidentialBuilding(
                    name,
                    buildingType,
                    new Position(positionX, positionY),
                    new Footprint(footprintWidth, footprintHeight),
                    floors,
                    constructionDate,
                    id
                );

                // Восстанавливаем износ
                while (building.Wear < wear)
                {
                    building.AccumulateWear(TimeSpan.FromDays(1));
                }

                // Восстанавливаем историю обслуживания
                if (buildingNode["maintenanceHistory"] is JsonArray maintenanceArray)
                {
                    foreach (var recordNode in maintenanceArray.OfType<JsonObject>())
                    {
                        var date = new DateTime(recordNode["date"]!.GetValue<long>());
                        var type = Enum.Parse<MaintenanceType>(recordNode["type"]!.GetValue<string>());
                        var cost = recordNode["cost"]!.GetValue<double>();
                        var wearReduction = recordNode["wearReduction"]!.GetValue<double>();
                        var description = recordNode["description"]!.GetValue<string>();

                        building.Maintain(type, cost, date);
                    }
                }

                // Добавляем в реестр
                _registry.TryAdd(building);
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но продолжаем загрузку
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке здания: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Добавляет жилое здание в реестр.
    /// </summary>
    public bool TryAddResidentialBuilding(ResidentialBuilding building)
    {
        if (_registry == null)
            throw new InvalidOperationException("Модуль не инициализирован");

        return _registry.TryAdd(building);
    }

    /// <summary>
    /// Удаляет здание из реестра.
    /// </summary>
    public bool RemoveBuilding(EntityId buildingId)
    {
        if (_registry == null)
            throw new InvalidOperationException("Модуль не инициализирован");

        return _registry.Remove(buildingId);
    }

    /// <summary>
    /// Получает реестр для прямого доступа (внутреннее использование).
    /// </summary>
    internal ResidentialBuildingRegistry? GetRegistry() => _registry;
}
