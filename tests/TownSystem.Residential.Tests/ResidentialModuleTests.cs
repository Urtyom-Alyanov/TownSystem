using TownSystem.Residential;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;
using TownSystem.SharedLib.Persistence;

namespace TownSystem.Residential.Tests;

public class ResidentialModuleTests
{
    private class MockMapService : IMapService
    {
        public uint Width => 100;
        public uint Height => 100;

        public bool InBounds(uint x, uint y) => x < Width && y < Height;
        public TerrainType GetTerrain(uint x, uint y) => TerrainType.Plain;
        public bool IsBuildable(uint x, uint y) => true;
        public SharedLib.Contracts.ResourceDeposit? GetDeposit(uint x, uint y) => null;
        public double Extract(uint x, uint y, double amount) => 0;
    }

    private class TestWorld : IWorld
    {
        private readonly Dictionary<Type, object> _services = new();
        public SimulationClock Clock { get; } = new();
        public IEventBus Events { get; } = new EventBus();
        public IReadOnlyList<ISimulationModule> Modules => new List<ISimulationModule>();

        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
        }

        public bool TryGetService<T>(out T? service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = null;
            return false;
        }
    }

    [Fact]
    public void Constructor_CreatesModule()
    {
        // Act
        var module = new ResidentialModule();

        // Assert
        Assert.Equal("Residential", module.Name);
        Assert.Equal("residential", module.SaveKey);
        Assert.Equal(-50, module.Order);
    }

    [Fact]
    public void Initialize_RegistersBuildingRegistry()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();

        // Act
        module.Initialize(world);

        // Assert
        var registry = world.GetService<IBuildingRegistry>();
        Assert.NotNull(registry);
    }

    [Fact]
    public void Initialize_WithNullWorld_ThrowsArgumentNullException()
    {
        // Arrange
        var module = new ResidentialModule();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => module.Initialize(null!));
    }

    [Fact]
    public void OnWorldReady_ObtainsMapService()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();
        module.Initialize(world);

        // Act
        module.OnWorldReady();

        // Assert - If this doesn't throw, the method succeeded
        // (We can't directly check _mapService since it's private)
    }

    [Fact]
    public void Tick_UpdatesWear()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();
        module.Initialize(world);
        module.OnWorldReady();

        var building = new ResidentialBuilding(
            "Test", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );
        module.TryAddResidentialBuilding(building);

        double wearBefore = building.Wear;

        // Act
        module.Tick(new TickContext(TimeSpan.FromDays(1), DateTime.Now));

        // Assert
        Assert.True(building.Wear > wearBefore);
    }

    [Fact]
    public void TryAddResidentialBuilding_AddsBuilding()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();
        module.Initialize(world);
        module.OnWorldReady();

        var building = new ResidentialBuilding(
            "Test", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );

        // Act
        bool result = module.TryAddResidentialBuilding(building);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RemoveBuilding_RemovesBuilding()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();
        module.Initialize(world);
        module.OnWorldReady();

        var building = new ResidentialBuilding(
            "Test", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );
        module.TryAddResidentialBuilding(building);

        // Act
        bool result = module.RemoveBuilding(building.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Save_SerializesBuildings()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();
        module.Initialize(world);
        module.OnWorldReady();

        var building = new ResidentialBuilding(
            "Test", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );
        module.TryAddResidentialBuilding(building);

        // Act
        var json = module.Save();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("buildings", json.ToString());
    }

    [Fact]
    public void Load_WithEmptyJson_DoesNotFail()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();
        module.Initialize(world);
        module.OnWorldReady();

        // Act & Assert - Should not throw
        module.Load(new System.Text.Json.Nodes.JsonObject());
    }

    [Fact]
    public void Save_WithNoBuildings_ReturnsEmptyJson()
    {
        // Arrange
        var world = new TestWorld();
        world.RegisterService<IMapService>(new MockMapService());
        var module = new ResidentialModule();
        module.Initialize(world);

        // Act
        var json = module.Save();

        // Assert
        Assert.NotNull(json);
        var buildingsArray = json["buildings"];
        Assert.Empty(buildingsArray!.AsArray());
    }

    private sealed class StubMapModule : ISimulationModule
    {
        public string Name => "Map";
        public int Order => -100;

        public void Initialize(IWorld world)
        {
            world.RegisterService<IMapService>(new MockMapService());
        }

        public void Tick(TickContext context) { }
    }

    [Fact]
    public void WorldStart_WhenResidentialRegisteredBeforeMap_InitializesAndWorks()
    {
        // Arrange: специально регистрируем ResidentialModule раньше модуля карты
        var world = new World();
        var residentialModule = new ResidentialModule();
        var mapModule = new StubMapModule();

        // Act
        world.RegisterModule(residentialModule);
        world.RegisterModule(mapModule);
        world.Start();

        var registry = world.GetService<IBuildingRegistry>();
        var building = new ResidentialBuilding(
            "Дом 1", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );

        // Assert: порядок модулей отсортирован (-100, затем -50), реестр рабочий и единственный
        Assert.Equal(new[] { "Map", "Residential" }, world.Modules.Select(m => m.Name));
        Assert.True(residentialModule.TryAddResidentialBuilding(building));
        Assert.Single(registry.All);
        Assert.Same(registry.Find(building.Id), building);
    }

    [Fact]
    public void OnWorldReady_KeepsSingleBuildingRegistryInstance()
    {
        // Arrange
        var world = new World();
        var residentialModule = new ResidentialModule();
        world.RegisterModule(residentialModule);
        var registryAfterInitialize = world.GetService<IBuildingRegistry>();

        world.RegisterModule(new StubMapModule());

        // Act
        world.Start();
        var registryAfterStart = world.GetService<IBuildingRegistry>();

        // Assert
        Assert.Same(registryAfterInitialize, registryAfterStart);
    }

    [Fact]
    public void TryAddResidentialBuilding_BeforeOnWorldReady_ThrowsInvalidOperationException()
    {
        // Arrange
        var world = new World();
        var residentialModule = new ResidentialModule();
        world.RegisterModule(residentialModule);

        var building = new ResidentialBuilding(
            "Дом 1", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            residentialModule.TryAddResidentialBuilding(building));
    }

    private sealed class ForeignIndustrialBuilding : IBuilding
    {
        public EntityId Id { get; } = EntityId.New();
        public string Name => "Завод";
        public BuildingCategory Category => BuildingCategory.Industrial;
        public Position Position => new(40, 40);
        public Footprint Footprint => new(4, 4);
        public uint Floors => 2;
        public uint Capacity => 50;
        public double Wear => 0.1;
    }

    [Fact]
    public void SaveAndLoad_ThroughSaveService_PreservesWearAndMaintenanceHistory()
    {
        // Arrange
        var world1 = new World();
        world1.RegisterModule(new StubMapModule());
        var module1 = new ResidentialModule();
        world1.RegisterModule(module1);
        world1.Start();

        var constructionDate = new DateTime(2015, 5, 10);
        var building = new ResidentialBuilding(
            "Дом с историей",
            ResidentialBuildingType.MidRiseApartment,
            new Position(12, 18),
            new Footprint(3, 3),
            5,
            constructionDate
        );

        // Накапливаем заметный износ (0.5) и проводим разные виды ремонта
        building.AccumulateWear(TimeSpan.FromDays(50_000));
        var d1 = new DateTime(2026, 2, 1);
        var d2 = new DateTime(2026, 4, 15);
        var d3 = new DateTime(2026, 6, 20);
        var d4 = new DateTime(2026, 8, 1);

        building.Maintain(MaintenanceType.Inspection, 300, d1, "Плановый осмотр");
        building.Maintain(MaintenanceType.Repair, 1200, d2, "Починка крыши");
        building.Maintain(MaintenanceType.MajorRepair, 8500, d3, "Капитальный ремонт фасада");
        building.Maintain(MaintenanceType.Emergency, 2500, d4, null);

        double expectedWear = building.Wear;
        double expectedTotalCost = building.TotalMaintenanceCost;
        DateTime? expectedLastDate = building.LastMaintenanceDate;

        Assert.True(module1.TryAddResidentialBuilding(building));
        string json = SaveService.SaveToJson(world1);

        // Act: загружаем в новый мир
        var world2 = new World();
        world2.RegisterModule(new StubMapModule());
        var module2 = new ResidentialModule();
        world2.RegisterModule(module2);
        world2.Start();

        SaveService.LoadFromJson(world2, json);

        // Assert
        var registry2 = world2.GetService<IBuildingRegistry>();
        var loaded = Assert.IsType<ResidentialBuilding>(registry2.Find(building.Id));

        Assert.Equal("Дом с историей", loaded.Name);
        Assert.Equal(ResidentialBuildingType.MidRiseApartment, loaded.BuildingType);
        Assert.Equal(new Position(12, 18), loaded.Position);
        Assert.Equal(new Footprint(3, 3), loaded.Footprint);
        Assert.Equal(5u, loaded.Floors);
        Assert.Equal(constructionDate, loaded.ConstructionDate);

        // Главное: износ не уменьшился повторно от истории ремонтов
        Assert.Equal(expectedWear, loaded.Wear, precision: 10);
        Assert.Equal(expectedTotalCost, loaded.TotalMaintenanceCost, precision: 10);
        Assert.Equal(expectedLastDate, loaded.LastMaintenanceDate);

        Assert.Equal(4, loaded.MaintenanceHistory.Count);
        Assert.Equal(MaintenanceType.Inspection, loaded.MaintenanceHistory[0].Type);
        Assert.Equal(300, loaded.MaintenanceHistory[0].Cost);
        Assert.Equal(0.0, loaded.MaintenanceHistory[0].WearReduction);
        Assert.Equal("Плановый осмотр", loaded.MaintenanceHistory[0].Description);

        Assert.Equal(MaintenanceType.Repair, loaded.MaintenanceHistory[1].Type);
        Assert.Equal("Починка крыши", loaded.MaintenanceHistory[1].Description);

        Assert.Equal(MaintenanceType.MajorRepair, loaded.MaintenanceHistory[2].Type);
        Assert.Equal(8500, loaded.MaintenanceHistory[2].Cost);
        Assert.Equal(0.3, loaded.MaintenanceHistory[2].WearReduction);
        Assert.Equal("Капитальный ремонт фасада", loaded.MaintenanceHistory[2].Description);

        Assert.Equal(MaintenanceType.Emergency, loaded.MaintenanceHistory[3].Type);
        Assert.Null(loaded.MaintenanceHistory[3].Description);
    }

    [Fact]
    public void Load_DoesNotRemoveForeignBuildingsAndDoesNotPublishEvents()
    {
        // Arrange
        var world = new World();
        world.RegisterModule(new StubMapModule());
        var module = new ResidentialModule();
        world.RegisterModule(module);
        world.Start();

        var registry = world.GetService<IBuildingRegistry>();

        var oldHouse = new ResidentialBuilding(
            "Старый дом", ResidentialBuildingType.PrivateHouse,
            new Position(5, 5), new Footprint(2, 2), 1, new DateTime(2020, 1, 1)
        );
        var foreignBuilding = new ForeignIndustrialBuilding();

        Assert.True(registry.TryAdd(oldHouse));
        Assert.True(registry.TryAdd(foreignBuilding));

        // Готовим сохранение с новым домом
        var newHouse = new ResidentialBuilding(
            "Новый дом", ResidentialBuildingType.LowRiseApartment,
            new Position(20, 20), new Footprint(2, 2), 3, new DateTime(2025, 1, 1)
        );
        var saveWorld = new World();
        saveWorld.RegisterModule(new StubMapModule());
        var saveModule = new ResidentialModule();
        saveWorld.RegisterModule(saveModule);
        saveWorld.Start();
        saveModule.TryAddResidentialBuilding(newHouse);
        string json = SaveService.SaveToJson(saveWorld);

        var publishedEvents = new List<IWorldEvent>();
        using var subAdded = world.Events.Subscribe<BuildingAdded>(e => publishedEvents.Add(e));
        using var subRemoved = world.Events.Subscribe<BuildingRemoved>(e => publishedEvents.Add(e));

        // Act
        SaveService.LoadFromJson(world, json);

        // Assert: старый жилой дом заменён, чужое здание осталось, события не публиковались
        Assert.Null(registry.Find(oldHouse.Id));
        Assert.NotNull(registry.Find(newHouse.Id));
        Assert.Same(foreignBuilding, registry.Find(foreignBuilding.Id));
        Assert.Equal(2, registry.All.Count);
        Assert.Empty(publishedEvents);
    }
}
