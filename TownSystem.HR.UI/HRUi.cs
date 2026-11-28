using System;
using System.Windows;

using TownSystem.HR.UI.ViewModels;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Modules;
using TownSystem.UILib.Mvvm;

namespace TownSystem.HR.UI;

public sealed class HRUi : IModuleUi
{
    public string Title => "Кадры";
    public int Order => 21;

    public ModuleViewModel CreateViewModel(IWorld world) => new HRViewModel(world);

    public ResourceDictionary Templates { get; } = new()
    {
        Source = new Uri("pack://application:,,,/TownSystem.HR.UI;component/Templates.xaml")
    };
}