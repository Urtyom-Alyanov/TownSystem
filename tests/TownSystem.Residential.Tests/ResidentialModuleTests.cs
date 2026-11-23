using TownSystem.Residential;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

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
}
