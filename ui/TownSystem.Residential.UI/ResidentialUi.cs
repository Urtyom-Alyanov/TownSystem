using System.Windows;

using TownSystem.Residential.UI.ViewModels;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Modules;
using TownSystem.UILib.Mvvm;

namespace TownSystem.Residential.UI;

/// <summary>
/// Точка интеграции интерфейса жилых зданий с общей оболочкой приложения.
/// </summary>
public sealed class ResidentialUi : IModuleUi
{
    public string Title => "Жилые здания";

    public int Order => -50;

    public ResourceDictionary Templates { get; } = new ResourceDictionary
    {
        Source = new Uri("pack://application:,,,/TownSystem.Residential.UI;component/Templates.xaml")
    };

    public ModuleViewModel CreateViewModel(IWorld world) => new ResidentialViewModel(world);
}