using System.Windows;

using TownSystem.Population.UI.ViewModels;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Modules;
using TownSystem.UILib.Mvvm;

namespace TownSystem.Population.UI;

/// <summary>
/// Точка интеграции интерфейса населения с общей оболочкой приложения.
/// </summary>
public sealed class PopulationUi : IModuleUi
{
    public string Title => "Население";

    public int Order => -48;

    public ResourceDictionary Templates { get; } = new ResourceDictionary
    {
        Source = new Uri("pack://application:,,,/TownSystem.Population.UI;component/Templates.xaml")
    };

    public ModuleViewModel CreateViewModel(IWorld world) => new PopulationViewModel(world);
}