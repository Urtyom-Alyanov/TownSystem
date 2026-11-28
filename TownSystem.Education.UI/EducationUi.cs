using System;
using System.Windows;

using TownSystem.Education.UI.ViewModels;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Modules;
using TownSystem.UILib.Mvvm;

namespace TownSystem.Education.UI;

public sealed class EducationUi : IModuleUi
{
    public string Title => "Образование";
    public int Order => 20;

    public ModuleViewModel CreateViewModel(IWorld world) => new EducationViewModel(world);

    public ResourceDictionary Templates { get; } = new()
    {
        Source = new Uri("pack://application:,,,/TownSystem.Education.UI;component/Templates.xaml")
    };
}