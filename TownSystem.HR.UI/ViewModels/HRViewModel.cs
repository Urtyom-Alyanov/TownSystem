using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

namespace TownSystem.HR.UI.ViewModels;

public sealed class HRViewModel : ModuleViewModel
{
    private readonly IHRService _hr;

    public EntityListViewModel<IEmployee> Employees { get; } = new();
    public EntityListViewModel<Vacancy> Vacancies { get; } = new();

    public HRViewModel(IWorld world) : base(world, "Кадры")
    {
        _hr = world.GetService<IHRService>();
        Refresh();
    }

    public override void Refresh()
    {
        Employees.Load(_hr.Employees);
        Vacancies.Load(_hr.Vacancies);
    }
}