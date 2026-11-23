using System.Text.Json.Nodes;
using TownSystem.Population;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

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
}
