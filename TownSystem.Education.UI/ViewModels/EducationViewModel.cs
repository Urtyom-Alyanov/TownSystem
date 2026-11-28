using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

namespace TownSystem.Education.UI.ViewModels;

public sealed class EducationViewModel : ModuleViewModel
{
    private readonly IEducationService _service;
    public EntityListViewModel<IEducationBuilding> Buildings { get; } = new();
    public EntityListViewModel<Enrollment> Enrollments { get; } = new();

    public EducationViewModel(IWorld world) : base(world, "Образование")
    {
        _service = world.GetService<IEducationService>();
        Refresh();
    }

    public override void Refresh()
    {
        Buildings.Load(_service.Buildings);
        Enrollments.Load(_service.Enrollments);
    }
}