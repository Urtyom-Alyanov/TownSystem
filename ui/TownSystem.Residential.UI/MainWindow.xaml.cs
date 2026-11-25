using System.Windows;

using TownSystem.Residential.UI.ViewModels;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Services;

namespace TownSystem.Residential.UI;

/// <summary>
/// Окно для автономного запуска и проверки вкладки жилых зданий.
/// </summary>
public partial class MainWindow : Window
{
    private readonly World world;
    private readonly SimulationRunner runner;
    private readonly ResidentialViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();

        world = new World();
        world.Clock.SetSpeed(86400); // 1 реальная секунда = 1 игровой день для наглядности износа
        world.RegisterModule(new PreviewMapModule());
        var resModule = new ResidentialModule();
        world.RegisterModule(resModule);
        world.Start();

        // Пара тестовых домов для наглядности при запуске
        var house1 = new ResidentialBuilding(
            "Коттедж «Сосновый»", ResidentialBuildingType.PrivateHouse,
            new Position(10, 10), new Footprint(2, 2), 1, new DateTime(2020, 5, 1)
        );
        var house2 = new ResidentialBuilding(
            "ЖК «Центральный»", ResidentialBuildingType.MidRiseApartment,
            new Position(16, 12), new Footprint(3, 3), 6, new DateTime(2016, 9, 15)
        );
        house2.AccumulateWear(TimeSpan.FromDays(25_000));
        house2.Maintain(MaintenanceType.Repair, 1000, new DateTime(2025, 4, 10), "Плановая починка крыши");

        resModule.TryAddResidentialBuilding(house1);
        resModule.TryAddResidentialBuilding(house2);

        viewModel = new ResidentialViewModel(world);
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

    private sealed class PreviewMapModule : ISimulationModule
    {
        public string Name => "PreviewMap";
        public int Order => -100;
        public void Initialize(IWorld targetWorld) => targetWorld.RegisterService<IMapService>(new PreviewMapService());
        public void Tick(TickContext context) { }
    }

    private sealed class PreviewMapService : IMapService
    {
        public uint Width => 100;
        public uint Height => 100;
        public bool InBounds(uint x, uint y) => x < Width && y < Height;
        public TerrainType GetTerrain(uint x, uint y) => TerrainType.Plain;
        public bool IsBuildable(uint x, uint y) => InBounds(x, y);
        public ResourceDeposit? GetDeposit(uint x, uint y) => null;
        public double Extract(uint x, uint y, double amount) => 0;
    }
}