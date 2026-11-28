using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Modules;
using TownSystem.UILib.Mvvm;

namespace TownSystem.UIApp;

public sealed class MainViewModel : ViewModelBase
{
    public IReadOnlyList<ModuleViewModel> Tabs { get; }
    public MainViewModel(IWorld world, ModuleCatalog catalog)
        => Tabs = catalog.CreateTabs(world);
}