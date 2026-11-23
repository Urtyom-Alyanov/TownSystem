using System.Text.Json.Nodes;
using TownSystem.Population;
using TownSystem.Residential;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.Residential.Tests;

/// <summary>
/// Интеграционные тесты для взаимодействия модулей Residential и Population.
/// Проверяют, что модули корректно работают через общие интерфейсы.
/// </summary>
public class IntegrationTests
{
    private class TestMapService : IMapService
    {
        public uint Width => 100;
        public uint Height => 100;

        public bool InBounds(uint x, uint y) => x < Width && y < Height;
        public bool IsBuildable(uint x, uint y) => InBounds(x, y);
        public TerrainType GetTerrain(uint x, uint y) => TerrainType.Plain;
        public ResourceDeposit? GetDeposit(uint x, uint y) => null;
        public double Extract(uint x, uint y, double amount) => 0;
    }

    private class TestEventBus : IEventBus
    {
        private readonly List<Action<IWorldEvent>> handlers = new();

        public void Publish(IWorldEvent worldEvent)
        {
            foreach (var handler in handlers)
            {
                handler(worldEvent);
            }
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : IWorldEvent
        {
            var wrappedHandler = (Action<IWorldEvent>)(@event =>
            {
                if (@event is T typedEvent)
                    handler(typedEvent);
            });

            handlers.Add(wrappedHandler);
            return new DisposableAction(() => handlers.Remove(wrappedHandler));
        }
    }

    private class DisposableAction : IDisposable
    {
        private readonly Action action;

        public DisposableAction(Action action)
        {
            this.action = action;
        }

        public void Dispose() => action();
    }

    private class TestWorld : IWorld
    {
        private readonly Dictionary<Type, object> services = new();
        private readonly TestEventBus eventBus = new();

        public SimulationClock Clock => new SimulationClock();
        public IEventBus Events => eventBus;
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
    public void Модули_регистрируют_сервисы_корректно()
    {
        // Подготовка
        var world = new TestWorld();
        world.RegisterService<IMapService>(new TestMapService());

        var residentialModule = new ResidentialModule();
        var populationModule = new PopulationModule();

        // Действие
        residentialModule.Initialize(world);
        populationModule.Initialize(world);

        // Проверка
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationServiceBase = world.GetService<IPopulationService>();

        Assert.NotNull(buildingRegistry);
        Assert.NotNull(populationServiceBase);
        
        // Убеждаемся, что это именно PopulationService с полным функционалом
        var populationService = populationServiceBase as PopulationService;
        Assert.NotNull(populationService);
    }

    [Fact]
    public void Граждане_могут_заселяться_в_здания()
    {
        // Подготовка
        var world = new TestWorld();
        world.RegisterService<IMapService>(new TestMapService());

        var residentialModule = new ResidentialModule();
        var populationModule = new PopulationModule();

        residentialModule.Initialize(world);
        populationModule.Initialize(world);
        residentialModule.OnWorldReady();
        populationModule.OnWorldReady();

        // Создаём здание
        var building = new ResidentialBuilding(
            "Дом 1",
            ResidentialBuildingType.PrivateHouse,
            new Position(10, 10),
            new Footprint(2, 2),
            1,
            DateTime.Now
        );

        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        var addedBuilding = buildingRegistry.TryAdd(building);
        Assert.True(addedBuilding);

        // Создаём семью и граждан
        var citizen1 = new Citizen(EntityId.New(), "Иван", 35, new Position(10, 10));
        var citizen2 = new Citizen(EntityId.New(), "Мария", 32, new Position(10, 10));

        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen1);
        family.AddMember(citizen2);

        var household = new Household(family, building.Id);

        // Действие
        populationService.RegisterCitizen(citizen1);
        populationService.RegisterCitizen(citizen2);
        populationService.RegisterFamily(family);
        populationService.RegisterHousehold(household);

        // Проверка
        Assert.Equal(2, populationService.People.Count);
        Assert.Equal(building.Id, populationService.GetResidence(citizen1.Id));
        Assert.Equal(building.Id, populationService.GetResidence(citizen2.Id));
    }

    [Fact]
    public void Сохранение_и_загрузка_обоих_модулей()
    {
        // Подготовка
        var world = new TestWorld();
        world.RegisterService<IMapService>(new TestMapService());

        var residentialModule = new ResidentialModule();
        var populationModule = new PopulationModule();

        residentialModule.Initialize(world);
        populationModule.Initialize(world);
        residentialModule.OnWorldReady();
        populationModule.OnWorldReady();

        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        // Добавляем данные
        var building = new ResidentialBuilding(
            "Многоквартирный дом",
            ResidentialBuildingType.MidRiseApartment,
            new Position(5, 5),
            new Footprint(3, 3),
            7,
            new DateTime(2010, 1, 1)
        );

        buildingRegistry.TryAdd(building);

        var citizen = new Citizen(EntityId.New(), "Петр", 40, new Position(5, 5));
        var family = new Family(EntityId.New(), "Сидоровы");
        family.AddMember(citizen);
        var household = new Household(family, building.Id);

        populationService.RegisterCitizen(citizen);
        populationService.RegisterFamily(family);
        populationService.RegisterHousehold(household);

        // Сохраняем
        var residentialSaved = residentialModule.Save();
        var populationSaved = populationModule.Save();

        // Создаём новые модули и загружаем
        var world2 = new TestWorld();
        world2.RegisterService<IMapService>(new TestMapService());

        var residentialModule2 = new ResidentialModule();
        var populationModule2 = new PopulationModule();

        residentialModule2.Initialize(world2);
        populationModule2.Initialize(world2);
        residentialModule2.OnWorldReady();
        populationModule2.OnWorldReady();

        // Загружаем
        residentialModule2.Load(residentialSaved);
        populationModule2.Load(populationSaved);

        var buildingRegistry2 = world2.GetService<IBuildingRegistry>();
        var populationService2 = (PopulationService)world2.GetService<IPopulationService>();

        // Проверка
        var loadedBuilding = buildingRegistry2.Find(building.Id);
        Assert.NotNull(loadedBuilding);
        Assert.Equal("Многоквартирный дом", loadedBuilding.Name);

        var loadedCitizen = populationService2.FindCitizen(citizen.Id);
        Assert.NotNull(loadedCitizen);
        Assert.Equal("Петр", loadedCitizen.Name);
        Assert.Equal(building.Id, populationService2.GetResidence(citizen.Id));
    }

    [Fact]
    public void Модули_правильно_обрабатывают_пустые_данные()
    {
        // Подготовка
        var world = new TestWorld();
        world.RegisterService<IMapService>(new TestMapService());

        var residentialModule = new ResidentialModule();
        var populationModule = new PopulationModule();

        residentialModule.Initialize(world);
        populationModule.Initialize(world);

        // Действие - пусто сохраняем и загружаем
        var residentialEmpty = residentialModule.Save();
        var populationEmpty = populationModule.Save();

        residentialModule.Load(residentialEmpty);
        populationModule.Load(populationEmpty);

        // Проверка - всё должно работать без ошибок
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        Assert.Empty(buildingRegistry.All);
        Assert.Empty(populationService.People);
    }

    [Fact]
    public void Множество_зданий_и_граждан_работают_вместе()
    {
        // Подготовка
        var world = new TestWorld();
        world.RegisterService<IMapService>(new TestMapService());

        var residentialModule = new ResidentialModule();
        var populationModule = new PopulationModule();

        residentialModule.Initialize(world);
        populationModule.Initialize(world);
        residentialModule.OnWorldReady();
        populationModule.OnWorldReady();

        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        // Создаём 3 здания
        var buildings = new List<ResidentialBuilding>();
        for (uint i = 0; i < 3; i++)
        {
            uint posX = 10 + i * 5;
            uint posY = 10;
            var building = new ResidentialBuilding(
                $"Дом {i + 1}",
                ResidentialBuildingType.PrivateHouse,
                new Position(posX, posY),
                new Footprint(2, 2),
                1,
                DateTime.Now
            );
            buildingRegistry.TryAdd(building);
            buildings.Add(building);
        }

        // Создаём 3 семьи по 2 человека
        for (uint i = 0; i < 3; i++)
        {
            uint posX = 10 + i * 5;
            uint posY = 10;
            var citizen1 = new Citizen(EntityId.New(), $"Гражданин {i * 2 + 1}", (uint)(30 + (int)i), new Position(posX, posY));
            var citizen2 = new Citizen(EntityId.New(), $"Гражданин {i * 2 + 2}", (uint)(28 + (int)i), new Position(posX, posY));

            var family = new Family(EntityId.New(), $"Семья {i + 1}");
            family.AddMember(citizen1);
            family.AddMember(citizen2);

            var household = new Household(family, buildings[(int)i].Id);

            populationService.RegisterCitizen(citizen1);
            populationService.RegisterCitizen(citizen2);
            populationService.RegisterFamily(family);
            populationService.RegisterHousehold(household);
        }

        // Проверка
        Assert.Equal(3, buildingRegistry.All.Count);
        Assert.Equal(6, populationService.People.Count);

        // Проверяем что каждый гражданин живёт в соответствующем доме
        var residentsOfHouse0 = populationService.GetResidentsOfBuilding(buildings[0].Id);
        Assert.Equal(2, residentsOfHouse0.Count);
    }
}
