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
        public SharedLib.Contracts.ResourceDeposit? GetDeposit(uint x, uint y) => null;
        public double Extract(uint x, uint y, double amount) => 0;
    }

    private class ForeignNonResidentialBuilding : IBuilding
    {
        public EntityId Id { get; } = EntityId.New();
        public string Name => "Завод";
        public BuildingCategory Category => BuildingCategory.Industrial;
        public Position Position => new(30, 30);
        public Footprint Footprint => new(3, 3);
        public uint Floors => 1;
        public uint Capacity => 50;
        public double Wear => 0;
    }

    private static (World world, ResidentialModule resModule, PopulationModule popModule) CreateRunningSimulation()
    {
        var world = new World();
        var stubMap = new StubMapModule();
        var residential = new ResidentialModule();
        var population = new PopulationModule();

        world.RegisterModule(stubMap);
        world.RegisterModule(residential);
        world.RegisterModule(population);
        world.Start();

        return (world, residential, population);
    }

    private sealed class StubMapModule : ISimulationModule
    {
        public string Name => "Map";
        public int Order => -100;
        public void Initialize(IWorld world) => world.RegisterService<IMapService>(new TestMapService());
        public void Tick(TickContext context) { }
    }

    [Fact]
    public void Модули_регистрируют_сервисы_корректно()
    {
        var (world, _, _) = CreateRunningSimulation();

        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationServiceBase = world.GetService<IPopulationService>();

        Assert.NotNull(buildingRegistry);
        Assert.NotNull(populationServiceBase);
        Assert.IsType<PopulationService>(populationServiceBase);
    }

    [Fact]
    public void Граждане_могут_заселяться_в_здания()
    {
        var (world, resModule, popModule) = CreateRunningSimulation();

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

        Assert.True(buildingRegistry.TryAdd(building));

        var citizen1 = new Citizen(EntityId.New(), "Иван", 35, new Position(10, 10));
        var citizen2 = new Citizen(EntityId.New(), "Мария", 32, new Position(10, 10));

        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen1);
        family.AddMember(citizen2);

        var household = new Household(family, building.Id);

        populationService.RegisterCitizen(citizen1);
        populationService.RegisterCitizen(citizen2);
        populationService.RegisterFamily(family);
        Assert.True(populationService.RegisterHousehold(household));

        Assert.Equal(2, populationService.People.Count);
        Assert.Equal(building.Id, citizen1.HomeId);
        Assert.Equal(building.Id, citizen2.HomeId);
        Assert.Equal(building.Id, populationService.GetResidence(citizen1.Id));
        Assert.Equal(building.Id, populationService.GetResidence(citizen2.Id));
    }

    [Fact]
    public void Заселение_превышающее_вместимость_отклоняется()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        // Частный дом на 1 этаж имеет вместимость 4 человека
        var smallHouse = new ResidentialBuilding(
            "Коттедж", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );
        Assert.True(buildingRegistry.TryAdd(smallHouse));
        Assert.Equal(4u, smallHouse.Capacity);

        // Создаём семью из 5 человек
        var bigFamily = new Family(EntityId.New(), "БольшаяСемья");
        for (int i = 0; i < 5; i++)
        {
            var citizen = new Citizen(EntityId.New(), $"Член {i}", 20, new Position(0, 0));
            populationService.RegisterCitizen(citizen);
            bigFamily.AddMember(citizen);
        }

        // Попытка заселить 5 человек в дом вместимостью 4 должна быть отклонена
        Assert.False(populationService.RegisterHousehold(new Household(bigFamily, smallHouse.Id)));
        Assert.Empty(populationService.Households);
        Assert.Empty(populationService.GetResidentsOfBuilding(smallHouse.Id));
    }

    [Fact]
    public void Переселение_проверяет_вместимость_нового_здания()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        var houseCapacity8 = new ResidentialBuilding(
            "Дом 8 мест", ResidentialBuildingType.LowRiseApartment,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );
        var houseCapacity4 = new ResidentialBuilding(
            "Дом 4 места", ResidentialBuildingType.PrivateHouse,
            new Position(20, 20), new Footprint(2, 2), 1, DateTime.Now
        );
        Assert.True(buildingRegistry.TryAdd(houseCapacity8));
        Assert.True(buildingRegistry.TryAdd(houseCapacity4));

        var family = new Family(EntityId.New(), "СемьяИз6");
        for (int i = 0; i < 6; i++)
        {
            var citizen = new Citizen(EntityId.New(), $"Член {i}", 20, new Position(0, 0));
            populationService.RegisterCitizen(citizen);
            family.AddMember(citizen);
        }

        Assert.True(populationService.RegisterHousehold(new Household(family, houseCapacity8.Id)));

        // Попытка переселиться в дом на 4 места отклоняется
        Assert.False(populationService.MoveHousehold(family.Id, houseCapacity4.Id));
        Assert.Equal(houseCapacity8.Id, family.Members.First().HomeId);
        Assert.Equal(6, populationService.GetResidentsOfBuilding(houseCapacity8.Id).Count);
        Assert.Empty(populationService.GetResidentsOfBuilding(houseCapacity4.Id));
    }

    [Fact]
    public void Заселение_в_нежилое_или_несуществующее_здание_отклоняется()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        var factory = new ForeignNonResidentialBuilding();
        Assert.True(buildingRegistry.TryAdd(factory));

        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        populationService.RegisterCitizen(citizen);
        var family = new Family(EntityId.New(), "Семья");
        family.AddMember(citizen);

        // В промышленное здание заселиться нельзя
        Assert.False(populationService.RegisterHousehold(new Household(family, factory.Id)));

        // В несуществующее здание заселиться нельзя
        Assert.False(populationService.RegisterHousehold(new Household(family, EntityId.New())));
        Assert.Null(citizen.HomeId);
    }

    [Fact]
    public void Снос_жилого_здания_автоматически_выселяет_всех_жильцов()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        var building = new ResidentialBuilding(
            "Под снос", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );
        Assert.True(buildingRegistry.TryAdd(building));

        var citizen1 = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var citizen2 = new Citizen(EntityId.New(), "Мария", 28, new Position(0, 0));
        populationService.RegisterCitizen(citizen1);
        populationService.RegisterCitizen(citizen2);

        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen1);
        family.AddMember(citizen2);

        Assert.True(populationService.RegisterHousehold(new Household(family, building.Id)));
        Assert.Equal(building.Id, citizen1.HomeId);

        // Действие: сносим здание через IBuildingRegistry
        Assert.True(buildingRegistry.Remove(building.Id));

        // Проверка: домохозяйство выселено, люди живы, но остались без жилья
        Assert.Null(buildingRegistry.Find(building.Id));
        Assert.Empty(populationService.Households);
        Assert.Null(citizen1.HomeId);
        Assert.Null(citizen2.HomeId);
        Assert.Null(populationService.GetResidence(citizen1.Id));
        Assert.Null(populationService.GetResidence(citizen2.Id));
        Assert.Equal(2, populationService.People.Count);
    }

    [Fact]
    public void Сохранение_и_загрузка_обоих_модулей()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        var building = new ResidentialBuilding(
            "Многоквартирный дом",
            ResidentialBuildingType.MidRiseApartment,
            new Position(5, 5),
            new Footprint(3, 3),
            7,
            new DateTime(2010, 1, 1)
        );

        Assert.True(buildingRegistry.TryAdd(building));

        var citizen = new Citizen(EntityId.New(), "Петр", 40, new Position(5, 5));
        var family = new Family(EntityId.New(), "Сидоровы");
        family.AddMember(citizen);
        var household = new Household(family, building.Id);

        populationService.RegisterCitizen(citizen);
        populationService.RegisterFamily(family);
        Assert.True(populationService.RegisterHousehold(household));

        var residentialSaved = world.Modules.OfType<ResidentialModule>().First().Save();
        var populationSaved = world.Modules.OfType<PopulationModule>().First().Save();

        var (world2, resModule2, popModule2) = CreateRunningSimulation();

        resModule2.Load(residentialSaved);
        popModule2.Load(populationSaved);

        var buildingRegistry2 = world2.GetService<IBuildingRegistry>();
        var populationService2 = (PopulationService)world2.GetService<IPopulationService>();

        var loadedBuilding = buildingRegistry2.Find(building.Id);
        Assert.NotNull(loadedBuilding);
        Assert.Equal("Многоквартирный дом", loadedBuilding.Name);

        var loadedCitizen = populationService2.FindCitizen(citizen.Id);
        Assert.NotNull(loadedCitizen);
        Assert.Equal("Петр", loadedCitizen.Name);
        Assert.Equal(building.Id, loadedCitizen.HomeId);
        Assert.Equal(building.Id, populationService2.GetResidence(citizen.Id));
    }

    [Fact]
    public void Модули_правильно_обрабатывают_пустые_данные()
    {
        var (world, resModule, popModule) = CreateRunningSimulation();

        var residentialEmpty = resModule.Save();
        var populationEmpty = popModule.Save();

        resModule.Load(residentialEmpty);
        popModule.Load(populationEmpty);

        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        Assert.Empty(buildingRegistry.All);
        Assert.Empty(populationService.People);
    }

    [Fact]
    public void Множество_зданий_и_граждан_работают_вместе()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

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
            Assert.True(buildingRegistry.TryAdd(building));
            buildings.Add(building);
        }

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
            Assert.True(populationService.RegisterHousehold(household));
        }

        Assert.Equal(3, buildingRegistry.All.Count);
        Assert.Equal(6, populationService.People.Count);

        var residentsOfHouse0 = populationService.GetResidentsOfBuilding(buildings[0].Id);
        Assert.Equal(2, residentsOfHouse0.Count);
    }

    [Fact]
    public void Бедствие_наносит_урон_жилому_зданию()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();

        var house = new ResidentialBuilding(
            "Дом уязвимый", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, DateTime.Now
        );
        buildingRegistry.TryAdd(house);

        double wearBefore = house.Wear;

        // Публикуем пожар тяжести 4 в этом здании
        world.Events.Publish(new EmergencyReported(
            Guid.NewGuid(), EmergencyKind.Fire, house.Position, Severity: 4, SubjectId: house.Id));

        Assert.True(house.Wear > wearBefore);
        Assert.Equal(wearBefore + 0.20, house.Wear, precision: 5);
    }

    [Fact]
    public void Такт_симуляции_пересчитывает_удовлетворенность_и_старит_граждан()
    {
        var (world, _, _) = CreateRunningSimulation();
        var buildingRegistry = world.GetService<IBuildingRegistry>();
        var populationService = (PopulationService)world.GetService<IPopulationService>();

        var house = new ResidentialBuilding(
            "Стареющий дом", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, new DateTime(2020, 1, 1)
        );
        buildingRegistry.TryAdd(house);

        var citizen = new Citizen(EntityId.New(), "Иван", 25, new Position(10, 10));
        populationService.RegisterCitizen(citizen);
        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen);
        var household = new Household(family, house.Id);
        populationService.RegisterHousehold(household);

        // Наносим дому высокий износ, чтобы вызвать недовольство и падение здоровья
        house.ApplyDamage(0.85);

        // Устанавливаем скорость симуляции 1 к 1, чтобы Advance не умножал дни на 60
        world.Clock.SetSpeed(1);

        // Прогоняем 365 игровых дней (ровно 1 игровой год)
        world.Update(TimeSpan.FromDays(365));

        // Гражданин постарел ровно на 1 год (с 25 до 26)
        Assert.Equal(26u, citizen.Age);
        // Удовлетворённость жильём упала из-за высокого износа
        Assert.True(household.Satisfaction < 0.5);
        // Здоровье в аварийном доме снизилось
        Assert.True(citizen.Health < 1.0);
    }
}