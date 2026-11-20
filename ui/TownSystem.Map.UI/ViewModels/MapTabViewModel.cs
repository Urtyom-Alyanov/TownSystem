using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

using MapModel = TownSystem.Map.Models.Map;

namespace TownSystem.Map.UI.ViewModels;

/// <summary>
/// Связывает готовую карту с вкладкой общего окна
/// </summary>
public sealed class MapTabViewModel : ModuleViewModel
{
    public MapViewModel MapViewModel { get; }

    // Передаём карте в окне те же клетки, с которыми работает симуляция
    public MapTabViewModel(IWorld world, MapModel map) : base(world, "Карта")
    {
        MapViewModel = new MapViewModel(map) { CanGenerateMap = false };
    }

    // После тактов обновляем карточку выбранного участка
    public override void Refresh()
    {
        MapCellViewModel? selected = MapViewModel.SelectedCell;
        if (selected != null)
            selected.Refresh(MapViewModel.Map[selected.X, selected.Y]);
    }
}