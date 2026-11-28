using System.Windows;

using TownSystem.HR;
using TownSystem.HR.UI;
using TownSystem.Education;
using TownSystem.Education.UI;
using TownSystem.Map.Services;
using TownSystem.Map.UI;
using TownSystem.SharedLib.Simulation;
using TownSystem.SharedLib.Contracts; // <-- Добавь это
using TownSystem.UILib.Modules;

namespace TownSystem.UIApp;

public partial class App : Application
{
    public World World { get; } = new();
    public ModuleCatalog Catalog { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Подсовываем фейковое население (заглушка)
        World.RegisterService<IPopulationService>(new FakePopulationService());

        // 2. Инициализируем модули
        var mapModule = new MapModule(new MapGenerator(42).Generate(100, 100));

        Catalog
            .Register(mapModule, new MapUi(mapModule))
            .Register(new EducationModule(), new EducationUi())
            .Register(new HRModule(), new HRUi());

        // 3. Запускаем мир
        Catalog.RegisterInto(World);
        Catalog.MergeTemplatesInto(Resources);

        var mainWindow = new MainWindow();
        mainWindow.DataContext = new MainViewModel(World, Catalog);
        mainWindow.Show();
    }
}