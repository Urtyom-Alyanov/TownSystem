using TownSystem.Residential;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.Residential.Tests;

public class ResidentialBuildingRegistryTests
{
    private static IBuilding CreateTestBuilding(
        string name = "Test Building",
        Position? position = null,
        Footprint? footprint = null,
        uint floors = 1)
    {
        return new ResidentialBuilding(
            name,
            ResidentialBuildingType.PrivateHouse,
            position ?? new Position(5, 5),
            footprint ?? new Footprint(2, 2),
            floors,
            DateTime.Now
        );
    }

    private class MockMapService : IMapService
    {
        public uint Width => 100;
        public uint Height => 100;

        public bool InBounds(uint x, uint y)
        {
            return x < Width && y < Height;
        }

        public TerrainType GetTerrain(uint x, uint y)
        {
            return TerrainType.Plain;
        }

        public SharedLib.Contracts.ResourceDeposit? GetDeposit(uint x, uint y)
        {
            return null;
        }

        public double Extract(uint x, uint y, double amount)
        {
            return 0;
        }

        public Func<uint, uint, bool>? IsBuildableFilter { get; set; }

        public bool IsBuildable(uint x, uint y)
        {
            return IsBuildableFilter?.Invoke(x, y) ?? true;
        }
    }

    private class MockEventBus : IEventBus
    {
        public List<IWorldEvent> PublishedEvents { get; } = new();

        public void Publish(IWorldEvent worldEvent)
        {
            PublishedEvents.Add(worldEvent);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : IWorldEvent
        {
            return new NoOpDisposable();
        }

        private class NoOpDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }

    [Fact]
    public void Constructor_WithValidServices_CreatesRegistry()
    {
        // Arrange & Act
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        // Assert
        Assert.NotNull(registry);
        Assert.Empty(registry.All);
    }

    [Fact]
    public void Constructor_WithNullEventBus_ThrowsArgumentNullException()
    {
        // Act & Assert
        var mapService = new MockMapService();
        Assert.Throws<ArgumentNullException>(() =>
            new ResidentialBuildingRegistry(null!, mapService)
        );
    }

    [Fact]
    public void Constructor_WithNullMapService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var eventBus = new MockEventBus();
        Assert.Throws<ArgumentNullException>(() =>
            new ResidentialBuildingRegistry(eventBus, null!)
        );
    }

    [Fact]
    public void TryAdd_WithValidBuilding_AddsSuccessfully()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        var building = CreateTestBuilding();

        // Act
        bool result = registry.TryAdd(building);

        // Assert
        Assert.True(result);
        Assert.Single(registry.All);
        Assert.Contains(building, registry.All);
    }

    [Fact]
    public void TryAdd_PublishesBuildingAddedEvent()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        var building = CreateTestBuilding();

        // Act
        registry.TryAdd(building);

        // Assert
        Assert.Single(eventBus.PublishedEvents);
        Assert.IsType<BuildingAdded>(eventBus.PublishedEvents[0]);
        var buildingAdded = (BuildingAdded)eventBus.PublishedEvents[0];
        Assert.Equal(building.Id, buildingAdded.BuildingId);
    }

    [Fact]
    public void TryAdd_WithDuplicateId_FailsSilently()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        var building = CreateTestBuilding();

        // Act
        registry.TryAdd(building);
        bool result = registry.TryAdd(building);

        // Assert
        Assert.False(result);
        Assert.Single(registry.All); // Всё ещё только одно здание
    }

    [Fact]
    public void TryAdd_OutOfMapBounds_Fails()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        
        // Позиция за границей карты (100x100)
        var building = CreateTestBuilding(position: new Position(99, 99), footprint: new Footprint(2, 2));

        // Act
        bool result = registry.TryAdd(building);

        // Assert
        Assert.False(result);
        Assert.Empty(registry.All);
    }

    [Fact]
    public void TryAdd_WithOverlap_Fails()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        var building1 = CreateTestBuilding(
            name: "Building 1",
            position: new Position(10, 10),
            footprint: new Footprint(3, 3)
        );
        var building2 = CreateTestBuilding(
            name: "Building 2",
            position: new Position(11, 11),  // Пересекается с Building 1
            footprint: new Footprint(2, 2)
        );

        // Act
        registry.TryAdd(building1);
        bool result = registry.TryAdd(building2);

        // Assert
        Assert.False(result);
        Assert.Single(registry.All); // Только первое здание
    }

    [Fact]
    public void TryAdd_AdjacentBuildings_Succeeds()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        var building1 = CreateTestBuilding(
            name: "Building 1",
            position: new Position(10, 10),
            footprint: new Footprint(2, 2)
        );
        var building2 = CreateTestBuilding(
            name: "Building 2",
            position: new Position(12, 10),  // Соседний дом без пересечения
            footprint: new Footprint(2, 2)
        );

        // Act
        registry.TryAdd(building1);
        bool result = registry.TryAdd(building2);

        // Assert
        Assert.True(result);
        Assert.Equal(2, registry.All.Count);
    }

    [Fact]
    public void Remove_WithExistingBuilding_RemovesSuccessfully()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        var building = CreateTestBuilding();
        registry.TryAdd(building);

        // Act
        bool result = registry.Remove(building.Id);

        // Assert
        Assert.True(result);
        Assert.Empty(registry.All);
    }

    [Fact]
    public void Remove_PublishesBuildingRemovedEvent()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        var building = CreateTestBuilding();
        registry.TryAdd(building);
        eventBus.PublishedEvents.Clear(); // Очищаем события добавления

        // Act
        registry.Remove(building.Id);

        // Assert
        Assert.Single(eventBus.PublishedEvents);
        Assert.IsType<BuildingRemoved>(eventBus.PublishedEvents[0]);
        var buildingRemoved = (BuildingRemoved)eventBus.PublishedEvents[0];
        Assert.Equal(building.Id, buildingRemoved.BuildingId);
    }

    [Fact]
    public void Remove_WithNonExistentBuilding_FailsSilently()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        // Act
        bool result = registry.Remove(EntityId.New());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Find_WithExistingId_ReturnsBuilding()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        var building = CreateTestBuilding();
        registry.TryAdd(building);

        // Act
        var found = registry.Find(building.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(building.Id, found!.Id);
    }

    [Fact]
    public void Find_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        // Act
        var found = registry.Find(EntityId.New());

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void FindNear_WithBuildingsInRadius_ReturnsThem()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        var center = new Position(50, 50);
        var building1 = CreateTestBuilding(
            name: "Near",
            position: new Position(51, 51) // Расстояние ~1.4 единицы
        );
        var building2 = CreateTestBuilding(
            name: "Far",
            position: new Position(60, 60) // Расстояние ~14 единиц
        );

        registry.TryAdd(building1);
        registry.TryAdd(building2);

        // Act
        var nearBuildings = registry.FindNear(center, 5.0).ToList();

        // Assert
        Assert.Single(nearBuildings);
        Assert.Equal(building1.Id, nearBuildings[0].Id);
    }

    [Fact]
    public void FindNear_WithNoBuildings_ReturnsEmpty()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        // Act
        var nearBuildings = registry.FindNear(new Position(50, 50), 10.0).ToList();

        // Assert
        Assert.Empty(nearBuildings);
    }

    [Fact]
    public void All_ReturnsReadOnlyCollection()
    {
        // Arrange
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);
        var building = CreateTestBuilding();
        registry.TryAdd(building);

        // Act
        var all = registry.All;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyCollection<IBuilding>>(all);
        Assert.Single(all);
    }

    private sealed class FakeExternalBuilding : IBuilding
    {
        public EntityId Id { get; } = EntityId.New();
        public string Name => "External";
        public BuildingCategory Category => BuildingCategory.Other;
        public required Position Position { get; init; }
        public required Footprint Footprint { get; init; }
        public uint Floors => 1;
        public uint Capacity => 10;
        public double Wear => 0;
    }

    [Theory]
    [InlineData(-0.5, 5.0, 2u, 2u)]
    [InlineData(5.0, -10.0, 2u, 2u)]
    [InlineData(double.NaN, 5.0, 2u, 2u)]
    [InlineData(double.PositiveInfinity, 5.0, 2u, 2u)]
    [InlineData(1e20, 5.0, 2u, 2u)]
    [InlineData(5.0, 5.0, 0u, 2u)]
    [InlineData(5.0, 5.0, 2u, 0u)]
    [InlineData(5.0, 5.0, uint.MaxValue, 1u)]
    public void TryAdd_WithInvalidGeometryOrOverflow_ReturnsFalse(double x, double y, uint width, uint height)
    {
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        var building = new FakeExternalBuilding
        {
            Position = new Position(x, y),
            Footprint = new Footprint(width, height)
        };

        Assert.False(registry.TryAdd(building));
        Assert.Empty(registry.All);
    }

    [Fact]
    public void TryAdd_WithFractionalCoordinatesCrossingMapEdge_ReturnsFalse()
    {
        var eventBus = new MockEventBus();
        var mapService = new MockMapService(); // 100x100
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        // Занимает [99.5, 100.5) — выходит за границу 100
        var building = CreateTestBuilding(position: new Position(99.5, 10.0), footprint: new Footprint(1, 1));

        Assert.False(registry.TryAdd(building));
        Assert.Empty(registry.All);
    }

    [Fact]
    public void TryAdd_WithFractionalCoordinatesTouchingUnbuildableCell_ReturnsFalse()
    {
        var eventBus = new MockEventBus();
        var mapService = new MockMapService
        {
            // Клетка (12, 10) занята водой / недоступна для строительства
            IsBuildableFilter = (x, y) => !(x == 12 && y == 10)
        };
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        // Занимает [10.5, 12.5) × [10, 12) — пересекает клетки 10, 11 и 12
        var building = CreateTestBuilding(position: new Position(10.5, 10.0), footprint: new Footprint(2, 2));

        Assert.False(registry.TryAdd(building));
        Assert.Empty(registry.All);
    }

    [Fact]
    public void TryAdd_WithFractionalAdjacentBuildings_SucceedsWithoutOverlap()
    {
        var eventBus = new MockEventBus();
        var mapService = new MockMapService();
        var registry = new ResidentialBuildingRegistry(eventBus, mapService);

        // [10.5, 12.5) и [12.5, 14.5) не пересекаются
        var b1 = CreateTestBuilding("B1", new Position(10.5, 10.0), new Footprint(2, 2));
        var b2 = CreateTestBuilding("B2", new Position(12.5, 10.0), new Footprint(2, 2));
        // [12.0, 14.0) пересекает оба
        var b3 = CreateTestBuilding("B3", new Position(12.0, 10.0), new Footprint(2, 2));

        Assert.True(registry.TryAdd(b1));
        Assert.True(registry.TryAdd(b2));
        Assert.False(registry.TryAdd(b3));
        Assert.Equal(2, registry.All.Count);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void FindNear_WithInvalidRadius_ThrowsImmediately(double invalidRadius)
    {
        var registry = new ResidentialBuildingRegistry(new MockEventBus(), new MockMapService());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            registry.FindNear(new Position(50, 50), invalidRadius));
    }

    [Fact]
    public void FindNear_MeasuresDistanceToBuildingFootprint()
    {
        var registry = new ResidentialBuildingRegistry(new MockEventBus(), new MockMapService());

        // Угол в (45, 50) — расстояние до угла 5.0, но правая грань в (49, 50) — расстояние 1.0
        var largeBuilding = CreateTestBuilding("Large", new Position(45, 50), new Footprint(4, 2));
        registry.TryAdd(largeBuilding);

        var near = registry.FindNear(new Position(50, 50), 2.0).ToList();

        Assert.Single(near);
        Assert.Equal(largeBuilding.Id, near[0].Id);
    }
}
