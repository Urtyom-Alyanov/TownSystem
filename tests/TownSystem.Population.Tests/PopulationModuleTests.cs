using System.Text.Json.Nodes;
using TownSystem.Population;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;
using TownSystem.SharedLib.Persistence;

namespace TownSystem.Population.Tests;

public class PopulationModuleTests
{
    private class MockEventBus : IEventBus
    {
        public void Publish(IWorldEvent worldEvent) { }
        public IDisposable Subscribe<T>(Action<T> handler) where T : IWorldEvent => new MockDisposable();
    }

    private class MockDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private class MockBuildingRegistry : IBuildingRegistry
    {
        public IReadOnlyCollection<IBuilding> All => new List<IBuilding>();
        public bool TryAdd(IBuilding building) => true;
        public bool Remove(EntityId id) => true;
        public IBuilding? Find(EntityId id) => null;
        public IEnumerable<IBuilding> FindNear(Position center, double radius) => Enumerable.Empty<IBuilding>();
    }

    private class MockWorld : IWorld
    {
        private readonly Dictionary<Type, object> services = new();

        public SimulationClock Clock => new SimulationClock();
        public IEventBus Events => new MockEventBus();
        public IReadOnlyList<ISimulationModule> Modules => new List<ISimulationModule>();

        public void RegisterService<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        public T GetService<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var service))
                return (T)service;
            throw new InvalidOperationException($"Сервис {typeof(T).Name} не зарегистрирован");
        }

        public bool TryGetService<T>(out T? service) where T : class
        {
            service = null;
            if (services.TryGetValue(typeof(T), out var svc))
            {
                service = (T)svc;
                return true;
            }
            return false;
        }
    }

    [Fact]
    public void Initialize_RegistersPopulationService()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();

        // Act
        module.Initialize(world);

        // Assert
        var service = world.GetService<IPopulationService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Initialize_ThrowsOnNullWorld()
    {
        // Arrange
        var module = new PopulationModule();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => module.Initialize(null!));
    }

    [Fact]
    public void OnWorldReady_GetsRegistry()
    {
        // Arrange
        var world = new MockWorld();
        world.RegisterService<IBuildingRegistry>(new MockBuildingRegistry());
        var module = new PopulationModule();

        // Act
        module.Initialize(world);
        module.OnWorldReady();

        // Assert - не должно быть исключений
        Assert.NotNull(module.GetService());
    }

    [Fact]
    public void Tick_DoesNotThrow()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        // Act & Assert
        module.Tick(new TickContext(TimeSpan.FromDays(1), DateTime.UtcNow));
    }

    [Fact]
    public void Save_ReturnsEmptyWhenNoData()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        // Act
        var saved = module.Save();

        // Assert
        Assert.IsType<JsonObject>(saved);
        var obj = (JsonObject)saved;
        Assert.NotNull(obj["citizens"]);
        Assert.NotNull(obj["families"]);
        Assert.NotNull(obj["households"]);
    }

    [Fact]
    public void Save_And_Load_RoundTrip()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        var service = module.GetService()!;
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(10, 20));
        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen);
        var household = new Household(family, EntityId.New());

        service.RegisterCitizen(citizen);
        service.RegisterFamily(family);
        service.RegisterHousehold(household);

        // Act
        var saved = module.Save();

        // Create new module and load
        var module2 = new PopulationModule();
        module2.Initialize(world);
        module2.Load(saved);

        var service2 = module2.GetService()!;

        // Assert
        var loadedCitizen = service2.FindCitizen(citizen.Id);
        Assert.NotNull(loadedCitizen);
        Assert.Equal("Иван", loadedCitizen.Name);
        Assert.Equal(30u, loadedCitizen.Age);

        var loadedFamily = service2.FindFamily(family.Id);
        Assert.NotNull(loadedFamily);
        Assert.Equal("Петровы", loadedFamily.Name);
        Assert.Single(loadedFamily.Members);

        var loadedHousehold = service2.FindHousehold(family.Id);
        Assert.NotNull(loadedHousehold);
    }

    [Fact]
    public void Load_WithInvalidData_DoesNotThrow()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        var invalidData = new JsonObject
        {
            ["citizens"] = new JsonArray(new JsonObject { ["id"] = "invalid" }),
            ["families"] = new JsonArray(),
            ["households"] = new JsonArray()
        };

        // Act & Assert - не должно быть исключений
        module.Load(invalidData);
    }

    [Fact]
    public void Load_ClearsExistingData()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        var service = module.GetService()!;
        var citizen1 = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        service.RegisterCitizen(citizen1);

        Assert.Single(service.People);

        // Create new data
        var newData = new JsonObject
        {
            ["citizens"] = new JsonArray(),
            ["families"] = new JsonArray(),
            ["households"] = new JsonArray()
        };

        // Act
        module.Load(newData);

        // Assert
        Assert.Empty(service.People);
    }

    [Fact]
    public void Module_HasCorrectProperties()
    {
        // Arrange & Act
        var module = new PopulationModule();

        // Assert
        Assert.Equal("Population", module.Name);
        Assert.Equal("population", module.SaveKey);
        Assert.Equal(-48, module.Order);
    }

    [Fact]
    public void Save_IncludesCitizenData()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        var service = module.GetService()!;
        var citizen = new Citizen(EntityId.New(), "Мария", 25, new Position(5, 10));
        citizen.Education = EducationLevel.College;
        citizen.Health = 0.75;
        service.RegisterCitizen(citizen);

        // Act
        var saved = module.Save();

        // Assert
        var obj = (JsonObject)saved;
        var citizensArray = (JsonArray)obj["citizens"]!;
        Assert.Single(citizensArray);

        var citizenObj = (JsonObject)citizensArray[0]!;
        Assert.Equal("Мария", citizenObj["name"]!.GetValue<string>());
        Assert.Equal(25u, citizenObj["age"]!.GetValue<uint>());
        Assert.Equal("College", citizenObj["education"]!.GetValue<string>());
    }

    [Fact]
    public void Save_IncludesFamilyData()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        var service = module.GetService()!;
        var citizen = new Citizen(EntityId.New(), "Петр", 35, new Position(0, 0));
        var family = new Family(EntityId.New(), "Сидоровы");
        family.AddMember(citizen);
        service.RegisterCitizen(citizen);
        service.RegisterFamily(family);

        // Act
        var saved = module.Save();

        // Assert
        var obj = (JsonObject)saved;
        var familiesArray = (JsonArray)obj["families"]!;
        Assert.Single(familiesArray);

        var familyObj = (JsonObject)familiesArray[0]!;
        Assert.Equal("Сидоровы", familyObj["name"]!.GetValue<string>());
    }

    [Fact]
    public void Save_IncludesHouseholdData()
    {
        // Arrange
        var world = new MockWorld();
        var module = new PopulationModule();
        module.Initialize(world);

        var service = module.GetService()!;
        var citizen = new Citizen(EntityId.New(), "Алекс", 40, new Position(0, 0));
        var family = new Family(EntityId.New(), "Смирновы");
        family.AddMember(citizen);
        var buildingId = EntityId.New();
        var household = new Household(family, buildingId);
        household.Satisfaction = 0.9;

        service.RegisterCitizen(citizen);
        service.RegisterFamily(family);
        service.RegisterHousehold(household);

        // Act
        var saved = module.Save();

        // Assert
        var obj = (JsonObject)saved;
        var householdsArray = (JsonArray)obj["households"]!;
        Assert.Single(householdsArray);

        var householdObj = (JsonObject)householdsArray[0]!;
        Assert.Equal(0.9, householdObj["satisfaction"]!.GetValue<double>());
    }

    private sealed class TestBuilding : IBuilding
    {
        public required EntityId Id { get; init; }
        public string Name => "Здание";
        public required BuildingCategory Category { get; init; }
        public Position Position => new(0, 0);
        public Footprint Footprint => new(2, 2);
        public uint Floors => 1;
        public uint Capacity => 10;
        public double Wear => 0;
    }

    private sealed class InMemoryBuildingRegistry : IBuildingRegistry
    {
        private readonly Dictionary<EntityId, IBuilding> buildings = new();
        public IReadOnlyCollection<IBuilding> All => buildings.Values;
        public bool TryAdd(IBuilding building) => buildings.TryAdd(building.Id, building);
        public bool Remove(EntityId id) => buildings.Remove(id);
        public IBuilding? Find(EntityId id) => buildings.TryGetValue(id, out var b) ? b : null;
        public IEnumerable<IBuilding> FindNear(Position center, double radius) => buildings.Values;
    }

    private sealed class StubBuildingRegistryModule(IBuildingRegistry registry) : ISimulationModule
    {
        public string Name => "StubBuildings";
        public int Order => -50;
        public void Initialize(IWorld world) => world.RegisterService(registry);
        public void Tick(TickContext context) { }
    }

    [Fact]
    public void SaveAndLoad_ThroughSaveService_RestoresAllFieldsAndRelations()
    {
        // Arrange
        var buildingId = EntityId.New();
        var workplaceId = EntityId.New();

        var registry1 = new InMemoryBuildingRegistry();
        registry1.TryAdd(new TestBuilding { Id = buildingId, Category = BuildingCategory.Residential });

        var world1 = new World();
        world1.RegisterModule(new StubBuildingRegistryModule(registry1));
        var popModule1 = new PopulationModule();
        world1.RegisterModule(popModule1);
        world1.Start();

        var service1 = popModule1.GetService()!;

        var citizen1 = new Citizen(EntityId.New(), "Иван", 34, new Position(12.5, 34.2))
        {
            WorkplaceId = workplaceId,
            Education = EducationLevel.University,
            Health = 0.72
        };
        var citizen2 = new Citizen(EntityId.New(), "Мария", 31, new Position(15.0, 20.0))
        {
            Education = EducationLevel.College,
            Health = 0.95
        };

        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen1);
        family.AddMember(citizen2);

        var moveInDate = new DateTime(2025, 3, 15, 10, 30, 0, DateTimeKind.Utc);
        var household = new Household(family, buildingId, moveInDate)
        {
            Satisfaction = 0.85
        };

        Assert.True(service1.RegisterCitizen(citizen1));
        Assert.True(service1.RegisterCitizen(citizen2));
        Assert.True(service1.RegisterFamily(family));
        Assert.True(service1.RegisterHousehold(household));

        string json = SaveService.SaveToJson(world1);

        // Act: загружаем в новый мир с тем же зданием
        var registry2 = new InMemoryBuildingRegistry();
        registry2.TryAdd(new TestBuilding { Id = buildingId, Category = BuildingCategory.Residential });

        var world2 = new World();
        world2.RegisterModule(new StubBuildingRegistryModule(registry2));
        var popModule2 = new PopulationModule();
        world2.RegisterModule(popModule2);
        world2.Start();

        SaveService.LoadFromJson(world2, json);

        // Assert
        var service2 = popModule2.GetService()!;
        Assert.Equal(2, service2.People.Count);
        Assert.Single(service2.Families);
        Assert.Single(service2.Households);

        var loaded1 = service2.FindCitizen(citizen1.Id);
        Assert.NotNull(loaded1);
        Assert.Equal("Иван", loaded1.Name);
        Assert.Equal(34u, loaded1.Age);
        Assert.Equal(new Position(12.5, 34.2), loaded1.Position);
        Assert.Equal(workplaceId, loaded1.WorkplaceId);
        Assert.Equal(EducationLevel.University, loaded1.Education);
        Assert.Equal(0.72, loaded1.Health, precision: 5);
        Assert.Equal(buildingId, loaded1.HomeId);

        var loaded2 = service2.FindCitizen(citizen2.Id);
        Assert.NotNull(loaded2);
        Assert.Null(loaded2.WorkplaceId);
        Assert.Equal(EducationLevel.College, loaded2.Education);
        Assert.Equal(0.95, loaded2.Health, precision: 5);
        Assert.Equal(buildingId, loaded2.HomeId);

        var loadedFamily = service2.FindFamily(family.Id);
        Assert.NotNull(loadedFamily);
        Assert.Equal("Петровы", loadedFamily.Name);
        Assert.Equal(2, loadedFamily.MemberCount);

        var loadedHousehold = service2.FindHousehold(family.Id);
        Assert.NotNull(loadedHousehold);
        Assert.Equal(buildingId, loadedHousehold.BuildingId);
        Assert.Equal(moveInDate, loadedHousehold.MoveInDate);
        Assert.Equal(0.85, loadedHousehold.Satisfaction, precision: 5);

        Assert.Equal(buildingId, service2.GetResidence(citizen1.Id));
        Assert.Equal(2, service2.GetResidentsOfBuilding(buildingId).Count);
    }

    [Fact]
    public void Load_WhenBuildingMissingInRegistry_SkipsHouseholdAndKeepsCitizensHomeless()
    {
        // Arrange: сохраняем мир со зданием
        var missingBuildingId = EntityId.New();
        var registry1 = new InMemoryBuildingRegistry();
        registry1.TryAdd(new TestBuilding { Id = missingBuildingId, Category = BuildingCategory.Residential });

        var world1 = new World();
        world1.RegisterModule(new StubBuildingRegistryModule(registry1));
        var popModule1 = new PopulationModule();
        world1.RegisterModule(popModule1);
        world1.Start();

        var service1 = popModule1.GetService()!;
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen);
        service1.RegisterCitizen(citizen);
        service1.RegisterFamily(family);
        service1.RegisterHousehold(new Household(family, missingBuildingId));

        string json = SaveService.SaveToJson(world1);

        // Act: в новом мире этого здания нет
        var world2 = new World();
        world2.RegisterModule(new StubBuildingRegistryModule(new InMemoryBuildingRegistry()));
        var popModule2 = new PopulationModule();
        world2.RegisterModule(popModule2);
        world2.Start();

        SaveService.LoadFromJson(world2, json);

        // Assert
        var service2 = popModule2.GetService()!;
        var loadedCitizen = service2.FindCitizen(citizen.Id);
        Assert.NotNull(loadedCitizen);
        Assert.Null(loadedCitizen.HomeId);
        Assert.Null(service2.GetResidence(citizen.Id));
        Assert.NotNull(service2.FindFamily(family.Id));
        Assert.Null(service2.FindHousehold(family.Id));
        Assert.Empty(service2.Households);
    }

    [Fact]
    public void Load_WhenBuildingIsNotResidential_SkipsHousehold()
    {
        // Arrange
        var buildingId = EntityId.New();
        var registry1 = new InMemoryBuildingRegistry();
        registry1.TryAdd(new TestBuilding { Id = buildingId, Category = BuildingCategory.Residential });

        var world1 = new World();
        world1.RegisterModule(new StubBuildingRegistryModule(registry1));
        var popModule1 = new PopulationModule();
        world1.RegisterModule(popModule1);
        world1.Start();

        var service1 = popModule1.GetService()!;
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen);
        service1.RegisterCitizen(citizen);
        service1.RegisterFamily(family);
        service1.RegisterHousehold(new Household(family, buildingId));

        string json = SaveService.SaveToJson(world1);

        // Act: в новом мире под этим ID числится промышленное здание
        var registry2 = new InMemoryBuildingRegistry();
        registry2.TryAdd(new TestBuilding { Id = buildingId, Category = BuildingCategory.Industrial });

        var world2 = new World();
        world2.RegisterModule(new StubBuildingRegistryModule(registry2));
        var popModule2 = new PopulationModule();
        world2.RegisterModule(popModule2);
        world2.Start();

        SaveService.LoadFromJson(world2, json);

        // Assert
        var service2 = popModule2.GetService()!;
        var loadedCitizen = service2.FindCitizen(citizen.Id)!;
        Assert.Null(loadedCitizen.HomeId);
        Assert.Empty(service2.Households);
    }

    [Fact]
    public void Load_AfterUnregisterCitizen_DoesNotResurrectDeletedCitizen()
    {
        // Arrange
        var buildingId = EntityId.New();
        var registry = new InMemoryBuildingRegistry();
        registry.TryAdd(new TestBuilding { Id = buildingId, Category = BuildingCategory.Residential });

        var world1 = new World();
        world1.RegisterModule(new StubBuildingRegistryModule(registry));
        var popModule1 = new PopulationModule();
        world1.RegisterModule(popModule1);
        world1.Start();

        var service1 = popModule1.GetService()!;
        var citizen1 = new Citizen(EntityId.New(), "Удалённый", 40, new Position(0, 0));
        var citizen2 = new Citizen(EntityId.New(), "Живой", 35, new Position(0, 0));
        var family = new Family(EntityId.New(), "Семья");
        family.AddMember(citizen1);
        family.AddMember(citizen2);

        service1.RegisterCitizen(citizen1);
        service1.RegisterCitizen(citizen2);
        service1.RegisterFamily(family);
        service1.RegisterHousehold(new Household(family, buildingId));
        service1.UnregisterCitizen(citizen1.Id);

        string json = SaveService.SaveToJson(world1);

        // Act
        var world2 = new World();
        world2.RegisterModule(new StubBuildingRegistryModule(registry));
        var popModule2 = new PopulationModule();
        world2.RegisterModule(popModule2);
        world2.Start();

        SaveService.LoadFromJson(world2, json);

        // Assert
        var service2 = popModule2.GetService()!;
        Assert.Single(service2.People);
        Assert.Null(service2.FindCitizen(citizen1.Id));
        Assert.NotNull(service2.FindCitizen(citizen2.Id));
        Assert.Single(service2.FindFamily(family.Id)!.Members);
    }

    [Fact]
    public void Load_CalledTwiceWithSameJson_DoesNotCreateDuplicates()
    {
        // Arrange
        var buildingId = EntityId.New();
        var registry = new InMemoryBuildingRegistry();
        registry.TryAdd(new TestBuilding { Id = buildingId, Category = BuildingCategory.Residential });

        var world1 = new World();
        world1.RegisterModule(new StubBuildingRegistryModule(registry));
        var popModule1 = new PopulationModule();
        world1.RegisterModule(popModule1);
        world1.Start();

        var service1 = popModule1.GetService()!;
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen);
        service1.RegisterCitizen(citizen);
        service1.RegisterFamily(family);
        service1.RegisterHousehold(new Household(family, buildingId));

        string json = SaveService.SaveToJson(world1);

        var world2 = new World();
        world2.RegisterModule(new StubBuildingRegistryModule(registry));
        var popModule2 = new PopulationModule();
        world2.RegisterModule(popModule2);
        world2.Start();

        // Act: загружаем дважды подряд
        SaveService.LoadFromJson(world2, json);
        SaveService.LoadFromJson(world2, json);

        // Assert
        var service2 = popModule2.GetService()!;
        Assert.Single(service2.People);
        Assert.Single(service2.Families);
        Assert.Single(service2.Households);
        Assert.Single(service2.FindFamily(family.Id)!.Members);
        Assert.Single(service2.GetResidentsOfBuilding(buildingId));
    }
}
