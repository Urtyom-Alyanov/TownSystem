using System.Windows;

using TownSystem.Population.UI.ViewModels;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Services;

namespace TownSystem.Population.UI;

/// <summary>
/// Окно для автономного запуска и проверки вкладки населения.
/// Не зависит от проекта TownSystem.Residential — использует только IBuildingRegistry из SharedLib.
/// </summary>
public partial class MainWindow : Window
{
    private readonly World world;
    private readonly SimulationRunner runner;
    private readonly PopulationViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();

        world = new World();
        world.Clock.SetSpeed(86400 * 30); // 1 реальная секунда = 30 игровых дней для проверки старения

        var previewBuildings = new PreviewBuildingModule();
        var popModule = new PopulationModule();

        world.RegisterModule(previewBuildings);
        world.RegisterModule(popModule);
        world.Start();

        var registry = world.GetService<IBuildingRegistry>();
        var house1 = new PreviewBuilding("Коттедж «Сосновый»", new Position(10, 10), 4, wear: 0.1);
        var house2 = new PreviewBuilding("ЖК «Центральный»", new Position(16, 12), 12, wear: 0.35);
        registry.TryAdd(house1);
        registry.TryAdd(house2);

        var service = popModule.GetService()!;
        var c1 = new Citizen(EntityId.New(), "Иван Петров", 32, new Position(10, 10)) { Education = EducationLevel.University };
        var c2 = new Citizen(EntityId.New(), "Мария Петрова", 29, new Position(10, 10)) { Education = EducationLevel.College };
        var c3 = new Citizen(EntityId.New(), "Алексей Смирнов", 24, new Position(0, 0)) { Education = EducationLevel.School };

        var family1 = new Family(EntityId.New(), "Петровы");
        family1.AddMember(c1);
        family1.AddMember(c2);

        var family2 = new Family(EntityId.New(), "Смирновы");
        family2.AddMember(c3);

        service.RegisterCitizen(c1);
        service.RegisterCitizen(c2);
        service.RegisterCitizen(c3);
        service.RegisterFamily(family1);
        service.RegisterFamily(family2);
        service.RegisterHousehold(new Household(family1, house1.Id, world.Clock.Now));

        viewModel = new PopulationViewModel(world);
        ModuleView.DataContext = viewModel;

        runner = new SimulationRunner(world);
        world.Events.Subscribe<TickCompleted>(_ => UpdateClockText());
        UpdateClockText();

        Closed += (_, _) =>
        {
            runner.Stop();
            viewModel.Dispose();
        };
    }

    private void ToggleSimulation(object sender, RoutedEventArgs e)
    {
        if (runner.IsRunning)
        {
            runner.Stop();
            ToggleSimButton.Content = "Старт симуляции";
        }
        else
        {
            runner.Start();
            ToggleSimButton.Content = "Пауза";
        }
    }

    private void UpdateClockText()
    {
        ClockText.Text = $"Время: {world.Clock.Now:dd.MM.yyyy}";
    }

    private sealed class PreviewBuildingModule : ISimulationModule
    {
        public string Name => "PreviewBuildings";
        public int Order => -50;
        public void Initialize(IWorld targetWorld) => targetWorld.RegisterService<IBuildingRegistry>(new PreviewBuildingRegistry());
        public void Tick(TickContext context) { }
    }

    private sealed class PreviewBuilding : IBuilding
    {
        public PreviewBuilding(string name, Position position, uint capacity, double wear)
        {
            Name = name;
            Position = position;
            Capacity = capacity;
            Wear = wear;
        }

        public EntityId Id { get; } = EntityId.New();
        public string Name { get; }
        public BuildingCategory Category => BuildingCategory.Residential;
        public Position Position { get; }
        public Footprint Footprint => new(2, 2);
        public uint Floors => 1;
        public uint Capacity { get; }
        public double Wear { get; }
    }

    private sealed class PreviewBuildingRegistry : IBuildingRegistry
    {
        private readonly Dictionary<EntityId, IBuilding> buildings = new();
        public IReadOnlyCollection<IBuilding> All => buildings.Values;
        public bool TryAdd(IBuilding building) => buildings.TryAdd(building.Id, building);
        public bool Remove(EntityId id) => buildings.Remove(id);
        public IBuilding? Find(EntityId id) => buildings.TryGetValue(id, out var b) ? b : null;
        public IEnumerable<IBuilding> FindNear(Position center, double radius) => buildings.Values;
    }
}