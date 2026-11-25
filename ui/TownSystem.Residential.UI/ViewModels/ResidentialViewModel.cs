using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;

using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

namespace TownSystem.Residential.UI.ViewModels;

/// <summary>
/// Модель представления вкладки жилых зданий.
/// </summary>
public sealed class ResidentialViewModel : ModuleViewModel
{
    public sealed record BuildingTypeOption(ResidentialBuildingType Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    public sealed record MaintenanceHistoryItem(
        string DateText,
        string TypeText,
        string CostText,
        string WearReductionText,
        string DescriptionText
    );

    private readonly IBuildingRegistry? buildingRegistry;
    private int buildingCount;
    private uint totalCapacity;
    private string averageWearText = "0 %";

    private string newBuildingName = "Жилой дом";
    private BuildingTypeOption selectedBuildingType;
    private string newPosX = "10";
    private string newPosY = "10";
    private string newWidth = "2";
    private string newHeight = "2";
    private string newFloors = "1";
    private string statusMessage = "Выберите здание из списка или постройте новое";
    private Brush statusBrush = new SolidColorBrush(Color.FromRgb(171, 192, 199));

    public ResidentialViewModel(IWorld world) : base(world, "Жилые здания")
    {
        world.TryGetService(out buildingRegistry);

        BuildingTypeOptions = new[]
        {
            new BuildingTypeOption(ResidentialBuildingType.PrivateHouse, "Частный дом"),
            new BuildingTypeOption(ResidentialBuildingType.LowRiseApartment, "Малоэтажный дом"),
            new BuildingTypeOption(ResidentialBuildingType.MidRiseApartment, "Среднеэтажный дом"),
            new BuildingTypeOption(ResidentialBuildingType.HighRiseApartment, "Многоэтажный дом"),
            new BuildingTypeOption(ResidentialBuildingType.Dormitory, "Общежитие")
        };
        selectedBuildingType = BuildingTypeOptions[0];

        Buildings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EntityListViewModel<ResidentialBuilding>.SelectedItem))
            {
                RefreshSelectedDetails();
                CommandManager.InvalidateRequerySuggested();
            }
        };

        BuildCommand = new RelayCommand(BuildHouse);
        InspectCommand = new RelayCommand(() => MaintainSelected(MaintenanceType.Inspection, 200, "Плановый осмотр"), () => HasSelection);
        RepairCommand = new RelayCommand(() => MaintainSelected(MaintenanceType.Repair, 1000, "Текущий ремонт"), () => HasSelection);
        MajorRepairCommand = new RelayCommand(() => MaintainSelected(MaintenanceType.MajorRepair, 5000, "Капитальный ремонт"), () => HasSelection);
        DemolishCommand = new RelayCommand(DemolishSelected, () => HasSelection);

        Refresh();
    }

    public EntityListViewModel<ResidentialBuilding> Buildings { get; } = new();

    public ObservableCollection<MaintenanceHistoryItem> MaintenanceHistoryItems { get; } = new();

    public IReadOnlyList<BuildingTypeOption> BuildingTypeOptions { get; }

    public int BuildingCount { get => buildingCount; private set => SetProperty(ref buildingCount, value); }

    public uint TotalCapacity { get => totalCapacity; private set => SetProperty(ref totalCapacity, value); }

    public string AverageWearText { get => averageWearText; private set => SetProperty(ref averageWearText, value); }

    public bool HasSelection => Buildings.SelectedItem != null;

    public string SelectedName => Buildings.SelectedItem?.Name ?? "Выберите здание";

    public string SelectedCoordinatesText => Buildings.SelectedItem is { } b
        ? $"X {b.Position.X:0.#} · Y {b.Position.Y:0.#}"
        : "Выберите здание";

    public string SelectedTypeText => Buildings.SelectedItem is { } b ? FormatBuildingType(b.BuildingType) : "—";

    public string SelectedFootprintText => Buildings.SelectedItem is { } b ? $"{b.Footprint.Width} × {b.Footprint.Height}" : "—";

    public string SelectedFloorsText => Buildings.SelectedItem is { } b ? $"{b.Floors}" : "—";

    public string SelectedCapacityText => Buildings.SelectedItem is { } b ? $"{b.Capacity} чел." : "—";

    public string SelectedAgeText => Buildings.SelectedItem is { } b ? $"{b.GetAgeInYears(World.Clock.Now)} лет" : "—";

    public string SelectedWearText => Buildings.SelectedItem is { } b ? $"{b.Wear * 100:0.0} %" : "—";

    public double SelectedWearPercent => Buildings.SelectedItem is { } b ? b.Wear * 100.0 : 0.0;

    public string SelectedConditionText => Buildings.SelectedItem is { } b ? FormatCondition(b.Condition) : "—";

    public Brush SelectedConditionBrush => Buildings.SelectedItem?.Condition switch
    {
        BuildingCondition.New => new SolidColorBrush(Color.FromRgb(112, 218, 188)),
        BuildingCondition.Good => new SolidColorBrush(Color.FromRgb(173, 207, 145)),
        BuildingCondition.Fair => new SolidColorBrush(Color.FromRgb(243, 225, 156)),
        BuildingCondition.NeedsRepair => new SolidColorBrush(Color.FromRgb(234, 185, 119)),
        BuildingCondition.Poor => new SolidColorBrush(Color.FromRgb(224, 128, 96)),
        BuildingCondition.Critical => new SolidColorBrush(Color.FromRgb(235, 97, 97)),
        _ => new SolidColorBrush(Color.FromRgb(171, 192, 199))
    };

    public string SelectedLastMaintenanceText => Buildings.SelectedItem?.LastMaintenanceDate is { } dt
        ? dt.ToString("dd.MM.yyyy")
        : "Не проводилось";

    public string SelectedTotalCostText => Buildings.SelectedItem is { } b ? $"{b.TotalMaintenanceCost:0} у.е." : "—";

    public string NewBuildingName { get => newBuildingName; set => SetProperty(ref newBuildingName, value); }

    public BuildingTypeOption SelectedBuildingType { get => selectedBuildingType; set => SetProperty(ref selectedBuildingType, value); }

    public string NewPosX { get => newPosX; set => SetProperty(ref newPosX, value); }

    public string NewPosY { get => newPosY; set => SetProperty(ref newPosY, value); }

    public string NewWidth { get => newWidth; set => SetProperty(ref newWidth, value); }

    public string NewHeight { get => newHeight; set => SetProperty(ref newHeight, value); }

    public string NewFloors { get => newFloors; set => SetProperty(ref newFloors, value); }

    public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }

    public Brush StatusBrush { get => statusBrush; private set => SetProperty(ref statusBrush, value); }

    public ICommand BuildCommand { get; }
    public ICommand InspectCommand { get; }
    public ICommand RepairCommand { get; }
    public ICommand MajorRepairCommand { get; }
    public ICommand DemolishCommand { get; }

    public override void Refresh()
    {
        if (buildingRegistry == null)
            return;

        var residential = buildingRegistry.All
            .OfType<ResidentialBuilding>()
            .OrderBy(b => b.Name)
            .ToList();

        // Перезагружаем коллекцию только если изменился состав зданий, чтобы не сбрасывать скролл
        if (!Buildings.Items.Select(b => b.Id).SequenceEqual(residential.Select(b => b.Id)))
        {
            Buildings.Load(residential);
            if (Buildings.SelectedItem == null && Buildings.Items.Count > 0)
                Buildings.SelectedItem = Buildings.Items[0];
        }

        BuildingCount = residential.Count;
        TotalCapacity = (uint)residential.Sum(b => (long)b.Capacity);
        AverageWearText = residential.Count > 0
            ? $"{residential.Average(b => b.Wear) * 100:0.0} %"
            : "0.0 %";

        RefreshSelectedDetails();
    }

    private void BuildHouse()
    {
        if (buildingRegistry == null)
        {
            SetStatus("Реестр зданий не найден в мире.", isError: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(NewBuildingName))
        {
            SetStatus("Введите название здания.", isError: true);
            return;
        }

        if (!TryParseDouble(NewPosX, out double x) || !TryParseDouble(NewPosY, out double y))
        {
            SetStatus("Некорректные координаты X или Y.", isError: true);
            return;
        }

        if (!uint.TryParse(NewWidth, out uint width) || !uint.TryParse(NewHeight, out uint height) || width == 0 || height == 0)
        {
            SetStatus("Ширина и высота должны быть целыми числами больше 0.", isError: true);
            return;
        }

        if (!uint.TryParse(NewFloors, out uint floors) || floors == 0)
        {
            SetStatus("Количество этажей должно быть больше 0.", isError: true);
            return;
        }

        try
        {
            var building = new ResidentialBuilding(
                NewBuildingName.Trim(),
                SelectedBuildingType.Value,
                new Position(x, y),
                new Footprint(width, height),
                floors,
                World.Clock.Now
            );

            if (!buildingRegistry.TryAdd(building))
            {
                SetStatus("Не удалось разместить здание: участок занят, вне карты или непригоден для застройки.", isError: true);
                return;
            }

            Refresh();
            Buildings.SelectedItem = building;
            SetStatus($"Построено здание «{building.Name}» ({building.Capacity} мест).", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void MaintainSelected(MaintenanceType type, double cost, string description)
    {
        if (Buildings.SelectedItem is not { } building)
            return;

        double reduced = building.Maintain(type, cost, World.Clock.Now, description);
        Refresh();
        SetStatus($"{description} выполнен. Износ снижен на {reduced * 100:0.0} %.", isError: false);
    }

    private void DemolishSelected()
    {
        if (buildingRegistry == null || Buildings.SelectedItem is not { } building)
            return;

        string name = building.Name;
        if (buildingRegistry.Remove(building.Id))
        {
            Refresh();
            SetStatus($"Здание «{name}» снесено.", isError: false);
        }
    }

    private void RefreshSelectedDetails()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedName));
        OnPropertyChanged(nameof(SelectedCoordinatesText));
        OnPropertyChanged(nameof(SelectedTypeText));
        OnPropertyChanged(nameof(SelectedFootprintText));
        OnPropertyChanged(nameof(SelectedFloorsText));
        OnPropertyChanged(nameof(SelectedCapacityText));
        OnPropertyChanged(nameof(SelectedAgeText));
        OnPropertyChanged(nameof(SelectedWearText));
        OnPropertyChanged(nameof(SelectedWearPercent));
        OnPropertyChanged(nameof(SelectedConditionText));
        OnPropertyChanged(nameof(SelectedConditionBrush));
        OnPropertyChanged(nameof(SelectedLastMaintenanceText));
        OnPropertyChanged(nameof(SelectedTotalCostText));

        MaintenanceHistoryItems.Clear();
        if (Buildings.SelectedItem is { } building)
        {
            foreach (var record in building.MaintenanceHistory.Reverse())
            {
                MaintenanceHistoryItems.Add(new MaintenanceHistoryItem(
                    record.Date.ToString("dd.MM.yyyy"),
                    FormatMaintenanceType(record.Type),
                    $"{record.Cost:0} у.е.",
                    $"-{record.WearReduction * 100:0.0} %",
                    string.IsNullOrWhiteSpace(record.Description) ? "Без описания" : record.Description
                ));
            }
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusBrush = isError
            ? new SolidColorBrush(Color.FromRgb(235, 115, 115))
            : new SolidColorBrush(Color.FromRgb(112, 218, 188));
    }

    private static bool TryParseDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatBuildingType(ResidentialBuildingType type) => type switch
    {
        ResidentialBuildingType.PrivateHouse => "Частный дом",
        ResidentialBuildingType.LowRiseApartment => "Малоэтажный",
        ResidentialBuildingType.MidRiseApartment => "Среднеэтажный",
        ResidentialBuildingType.HighRiseApartment => "Многоэтажный",
        ResidentialBuildingType.Dormitory => "Общежитие",
        _ => type.ToString()
    };

    private static string FormatCondition(BuildingCondition condition) => condition switch
    {
        BuildingCondition.New => "Новое",
        BuildingCondition.Good => "Хорошее",
        BuildingCondition.Fair => "Удовлетворительное",
        BuildingCondition.NeedsRepair => "Требует ремонта",
        BuildingCondition.Poor => "Плохое",
        BuildingCondition.Critical => "Критическое",
        _ => condition.ToString()
    };

    private static string FormatMaintenanceType(MaintenanceType type) => type switch
    {
        MaintenanceType.Inspection => "Осмотр",
        MaintenanceType.Repair => "Текущий ремонт",
        MaintenanceType.MajorRepair => "Капитальный ремонт",
        MaintenanceType.Emergency => "Экстренный ремонт",
        _ => type.ToString()
    };
}