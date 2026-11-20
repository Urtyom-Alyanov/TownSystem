using System.Windows;

using TownSystem.Map.Services;
using TownSystem.Map.UI.ViewModels;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Modules;
using TownSystem.UILib.Mvvm;

namespace TownSystem.Map.UI;

/// <summary>
/// Даёт общей оболочке заголовок, данные и шаблон вкладки карты
/// </summary>
public sealed class MapUi : IModuleUi
{
    private readonly MapModule module;

    public string Title => "Карта";

    public ResourceDictionary Templates { get; } = new ResourceDictionary
    {
        Source = new Uri("pack://application:,,,/TownSystem.Map.UI;component/Templates.xaml")
    };

    // Берём карту из того модуля, который регистрируется в симуляции
    public MapUi(MapModule module)
    {
        this.module = module;
    }

    // Создаём данные для вкладки карты
    public ModuleViewModel CreateViewModel(IWorld world)
    {
        return new MapTabViewModel(world, module.Map);
    }
}