using System.Windows;

using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

namespace TownSystem.UILib.Modules;

/// <summary>
/// Каталог модулей приложения. Оболочка (вариант 6) создаёт его один раз; каждый модуль добавляет себя
/// одной строкой: <c>catalog.Register(new PoliceModule(), new PoliceUi())</c>.
/// </summary>
public sealed class ModuleCatalog
{
    private readonly List<(ISimulationModule Sim, IModuleUi? Ui)> _entries = new();

    /// <summary>Добавляет модуль симуляции и (необязательно) его интерфейс.</summary>
    public ModuleCatalog Register(ISimulationModule simulation, IModuleUi? ui = null)
    {
        _entries.Add((simulation, ui));
        return this;
    }

    /// <summary>Регистрирует все модули в мире и запускает мир (<see cref="World.Start"/>).</summary>
    public void RegisterInto(World world)
    {
        foreach (var (sim, _) in _entries) world.RegisterModule(sim);
        world.Start();
    }

    /// <summary>Подключает шаблоны View всех модулей к ресурсам приложения.</summary>
    public void MergeTemplatesInto(ResourceDictionary appResources)
    {
        foreach (var (_, ui) in _entries)
            if (ui?.Templates is { } templates) appResources.MergedDictionaries.Add(templates);
    }

    /// <summary>Создаёт ViewModel всех вкладок в порядке <see cref="IModuleUi.Order"/>.</summary>
    public IReadOnlyList<ModuleViewModel> CreateTabs(IWorld world) =>
        _entries.Select(e => e.Ui).OfType<IModuleUi>()
                .OrderBy(u => u.Order)
                .Select(u => u.CreateViewModel(world))
                .ToList();
}