using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Simulation;
using TownSystem.UILib.Mvvm;

namespace TownSystem.Population.UI.ViewModels;

/// <summary>
/// Модель представления вкладки населения.
/// </summary>
public sealed class PopulationViewModel : ModuleViewModel
{
    public sealed record EducationOption(EducationLevel Value, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    public sealed record BuildingOption(EntityId Id, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private readonly PopulationService? populationService;
    private readonly IBuildingRegistry? buildingRegistry;

    private string selectedSection = "Горожане";
    private Family? selectedFamily;
    private Household? selectedHousehold;

    private int citizenCount;
    private int familyCount;
    private int householdCount;
    private int homelessCount;

    private string newCitizenName = "Новый житель";
    private string newCitizenAge = "25";
    private EducationOption selectedEducationOption;
    private string newFamilyName = "Семья";

    private Family? selectedTargetFamilyForCitizen;
    private BuildingOption? selectedTargetBuildingForFamily;
    private BuildingOption? selectedTargetBuildingForHousehold;
    private Family? selectedSettleFamily;
    private BuildingOption? selectedSettleBuilding;

    private string statusMessage = "Выберите раздел и объект для управления";
    private Brush statusBrush = new SolidColorBrush(Color.FromRgb(171, 192, 199));

    public PopulationViewModel(IWorld world) : base(world, "Население")
    {
        if (world.TryGetService<IPopulationService>(out var popService))
            populationService = popService as PopulationService;

        world.TryGetService(out buildingRegistry);

        EducationOptions = new[]
        {
            new EducationOption(EducationLevel.None, "Без образования"),
            new EducationOption(EducationLevel.School, "Школа"),
            new EducationOption(EducationLevel.College, "Колледж"),
            new EducationOption(EducationLevel.University, "Университет")
        };
        selectedEducationOption = EducationOptions[0];

        Citizens.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EntityListViewModel<Citizen>.SelectedItem))
            {
                RefreshSelectedDetails();
                CommandManager.InvalidateRequerySuggested();
            }
        };

        AddCitizenCommand = new RelayCommand(AddCitizen);
        RemoveCitizenCommand = new RelayCommand(RemoveSelectedCitizen, () => Citizens.SelectedItem != null);
        AddCitizenToFamilyCommand = new RelayCommand(AddCitizenToFamily, () => Citizens.SelectedItem != null && SelectedTargetFamilyForCitizen != null);
        RemoveCitizenFromFamilyCommand = new RelayCommand(RemoveCitizenFromFamily, () => Citizens.SelectedItem != null && FindFamilyOfCitizen(Citizens.SelectedItem.Id) != null);

        AddFamilyCommand = new RelayCommand(AddFamily);
        SettleOrMoveFamilyCommand = new RelayCommand(SettleOrMoveSelectedFamily, () => SelectedFamily != null && SelectedTargetBuildingForFamily != null);
        EvictFamilyCommand = new RelayCommand(EvictSelectedFamily, () => SelectedFamily != null && populationService?.FindHousehold(SelectedFamily.Id) != null);
        RemoveFamilyCommand = new RelayCommand(RemoveSelectedFamily, () => SelectedFamily != null);

        SettleHouseholdCommand = new RelayCommand(SettleFromHouseholdTab, () => SelectedSettleFamily != null && SelectedSettleBuilding != null);
        MoveHouseholdCommand = new RelayCommand(MoveSelectedHousehold, () => SelectedHousehold != null && SelectedTargetBuildingForHousehold != null);
        EvictHouseholdCommand = new RelayCommand(EvictSelectedHousehold, () => SelectedHousehold != null);

        Refresh();
    }

    public string[] Sections { get; } = { "Горожане", "Семьи", "Домохозяйства" };

    public string SelectedSection
    {
        get => selectedSection;
        set
        {
            if (value == null || selectedSection == value)
                return;

            SetProperty(ref selectedSection, value);
        }
    }

    public EntityListViewModel<Citizen> Citizens { get; } = new();

    public ObservableCollection<Family> Families { get; } = new();

    public ObservableCollection<Household> Households { get; } = new();

    public ObservableCollection<BuildingOption> ResidentialBuildings { get; } = new();

    public ObservableCollection<Citizen> SelectedFamilyMembers { get; } = new();

    public ObservableCollection<Citizen> SelectedHouseholdMembers { get; } = new();

    public IReadOnlyList<EducationOption> EducationOptions { get; }

    public Family? SelectedFamily
    {
        get => selectedFamily;
        set
        {
            if (SetProperty(ref selectedFamily, value))
            {
                RefreshSelectedDetails();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public Household? SelectedHousehold
    {
        get => selectedHousehold;
        set
        {
            if (SetProperty(ref selectedHousehold, value))
            {
                RefreshSelectedDetails();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public int CitizenCount { get => citizenCount; private set => SetProperty(ref citizenCount, value); }
    public int FamilyCount { get => familyCount; private set => SetProperty(ref familyCount, value); }
    public int HouseholdCount { get => householdCount; private set => SetProperty(ref householdCount, value); }
    public int HomelessCount { get => homelessCount; private set => SetProperty(ref homelessCount, value); }

    public string NewCitizenName { get => newCitizenName; set => SetProperty(ref newCitizenName, value); }
    public string NewCitizenAge { get => newCitizenAge; set => SetProperty(ref newCitizenAge, value); }
    public EducationOption SelectedEducationOption { get => selectedEducationOption; set => SetProperty(ref selectedEducationOption, value); }
    public string NewFamilyName { get => newFamilyName; set => SetProperty(ref newFamilyName, value); }

    public Family? SelectedTargetFamilyForCitizen { get => selectedTargetFamilyForCitizen; set => SetProperty(ref selectedTargetFamilyForCitizen, value); }
    public BuildingOption? SelectedTargetBuildingForFamily { get => selectedTargetBuildingForFamily; set => SetProperty(ref selectedTargetBuildingForFamily, value); }
    public BuildingOption? SelectedTargetBuildingForHousehold { get => selectedTargetBuildingForHousehold; set => SetProperty(ref selectedTargetBuildingForHousehold, value); }
    public Family? SelectedSettleFamily { get => selectedSettleFamily; set => SetProperty(ref selectedSettleFamily, value); }
    public BuildingOption? SelectedSettleBuilding { get => selectedSettleBuilding; set => SetProperty(ref selectedSettleBuilding, value); }

    public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
    public Brush StatusBrush { get => statusBrush; private set => SetProperty(ref statusBrush, value); }

    // Свойства карточки выбранного горожанина
    public string SelectedCitizenName => Citizens.SelectedItem?.Name ?? "Выберите жителя";
    public string SelectedCitizenIdText => Citizens.SelectedItem is { } c ? $"ID {c.Id}" : "—";
    public string SelectedCitizenAgeText => Citizens.SelectedItem is { } c ? $"{c.Age} лет" : "—";
    public string SelectedCitizenHealthText => Citizens.SelectedItem is { } c ? $"{c.Health * 100:0} %" : "—";
    public string SelectedCitizenEducationText => Citizens.SelectedItem is { } c ? FormatEducation(c.Education) : "—";
    public string SelectedCitizenPositionText => Citizens.SelectedItem is { } c ? c.Position.ToString() : "—";
    public string SelectedCitizenFamilyText => Citizens.SelectedItem is { } c
        ? (FindFamilyOfCitizen(c.Id)?.Name ?? "Без семьи")
        : "—";
    public string SelectedCitizenHomeText => Citizens.SelectedItem is { } c
        ? (c.HomeId is { } homeId ? GetBuildingDisplayName(homeId) : "Без жилья")
        : "—";

    // Свойства карточки выбранной семьи
    public string SelectedFamilyName => SelectedFamily?.Name ?? "Выберите семью";
    public string SelectedFamilyMembersCountText => SelectedFamily is { } f ? $"{f.MemberCount} чел." : "—";
    public string SelectedFamilyMinRoomsText => SelectedFamily is { } f ? $"{f.MinimalRoomCount} комн." : "—";
    public string SelectedFamilyHomeText => SelectedFamily is { } f && populationService?.FindHousehold(f.Id) is { } h
        ? GetBuildingDisplayName(h.BuildingId)
        : "Не заселена";
    public string SelectedFamilySatisfactionText => SelectedFamily is { } f && populationService?.FindHousehold(f.Id) is { } h
        ? $"{h.Satisfaction * 100:0} %"
        : "—";
    public string SettleOrMoveFamilyButtonText => SelectedFamily != null && populationService?.FindHousehold(SelectedFamily.Id) != null
        ? "Переселить семью"
        : "Заселить семью";

    // Свойства карточки выбранного домохозяйства
    public string SelectedHouseholdFamilyName => SelectedHousehold?.Family.Name ?? "Выберите домохозяйство";
    public string SelectedHouseholdBuildingName => SelectedHousehold is { } h ? GetBuildingDisplayName(h.BuildingId) : "—";
    public string SelectedHouseholdMoveInDateText => SelectedHousehold is { } h ? h.MoveInDate.ToString("dd.MM.yyyy") : "—";
    public string SelectedHouseholdSatisfactionText => SelectedHousehold is { } h ? $"{h.Satisfaction * 100:0} %" : "—";
    public string SelectedHouseholdOccupancyText => SelectedHousehold is { } h
        ? FormatBuildingOccupancy(h.BuildingId)
        : "—";

    public ICommand AddCitizenCommand { get; }
    public ICommand RemoveCitizenCommand { get; }
    public ICommand AddCitizenToFamilyCommand { get; }
    public ICommand RemoveCitizenFromFamilyCommand { get; }

    public ICommand AddFamilyCommand { get; }
    public ICommand SettleOrMoveFamilyCommand { get; }
    public ICommand EvictFamilyCommand { get; }
    public ICommand RemoveFamilyCommand { get; }

    public ICommand SettleHouseholdCommand { get; }
    public ICommand MoveHouseholdCommand { get; }
    public ICommand EvictHouseholdCommand { get; }

    public override void Refresh()
    {
        if (populationService == null)
            return;

        var currentCitizens = populationService.People
            .OfType<Citizen>()
            .OrderBy(c => c.Name)
            .ToList();

        if (!Citizens.Items.Select(c => (c.Id, c.Age, c.HomeId)).SequenceEqual(currentCitizens.Select(c => (c.Id, c.Age, c.HomeId))))
        {
            Citizens.Load(currentCitizens);
            if (Citizens.SelectedItem == null && Citizens.Items.Count > 0)
                Citizens.SelectedItem = Citizens.Items[0];
        }

        var currentFamilies = populationService.Families
            .OrderBy(f => f.Name)
            .ToList();

        if (!Families.Select(f => (f.Id, f.MemberCount)).SequenceEqual(currentFamilies.Select(f => (f.Id, f.MemberCount))))
        {
            var prevFamilyId = SelectedFamily?.Id;
            var prevCitizenTargetId = SelectedTargetFamilyForCitizen?.Id;
            var prevSettleFamilyId = SelectedSettleFamily?.Id;

            Families.Clear();
            foreach (var family in currentFamilies)
                Families.Add(family);

            SelectedFamily = prevFamilyId is { } fid
                ? Families.FirstOrDefault(f => f.Id == fid) ?? Families.FirstOrDefault()
                : Families.FirstOrDefault();

            SelectedTargetFamilyForCitizen = prevCitizenTargetId is { } ctid
                ? Families.FirstOrDefault(f => f.Id == ctid) ?? Families.FirstOrDefault()
                : Families.FirstOrDefault();

            SelectedSettleFamily = prevSettleFamilyId is { } sfid
                ? Families.FirstOrDefault(f => f.Id == sfid) ?? Families.FirstOrDefault()
                : Families.FirstOrDefault();
        }

        var currentHouseholds = populationService.Households
            .OrderBy(h => h.Family.Name)
            .ToList();

        if (!Households.Select(h => (h.Family.Id, h.BuildingId, h.Family.MemberCount))
            .SequenceEqual(currentHouseholds.Select(h => (h.Family.Id, h.BuildingId, h.Family.MemberCount))))
        {
            var prevHouseholdFamilyId = SelectedHousehold?.Family.Id;

            Households.Clear();
            foreach (var household in currentHouseholds)
                Households.Add(household);

            SelectedHousehold = prevHouseholdFamilyId is { } hfid
                ? Households.FirstOrDefault(h => h.Family.Id == hfid) ?? Households.FirstOrDefault()
                : Households.FirstOrDefault();
        }

        RefreshResidentialBuildings();

        CitizenCount = currentCitizens.Count;
        FamilyCount = currentFamilies.Count;
        HouseholdCount = currentHouseholds.Count;
        HomelessCount = currentCitizens.Count(c => c.HomeId == null);

        RefreshSelectedDetails();
    }

    private void RefreshResidentialBuildings()
    {
        if (buildingRegistry == null || populationService == null)
            return;

        var options = buildingRegistry.All
            .Where(b => b.Category == BuildingCategory.Residential)
            .OrderBy(b => b.Name)
            .Select(b =>
            {
                int occupied = populationService.GetResidentsOfBuilding(b.Id).Count;
                return new BuildingOption(b.Id, $"{b.Name} ({occupied}/{b.Capacity} мест)");
            })
            .ToList();

        if (!ResidentialBuildings.Select(o => (o.Id, o.DisplayName)).SequenceEqual(options.Select(o => (o.Id, o.DisplayName))))
        {
            var prevFamilyTarget = SelectedTargetBuildingForFamily?.Id;
            var prevHouseholdTarget = SelectedTargetBuildingForHousehold?.Id;
            var prevSettleTarget = SelectedSettleBuilding?.Id;

            ResidentialBuildings.Clear();
            foreach (var option in options)
                ResidentialBuildings.Add(option);

            SelectedTargetBuildingForFamily = prevFamilyTarget is { } id1
                ? ResidentialBuildings.FirstOrDefault(b => b.Id == id1) ?? ResidentialBuildings.FirstOrDefault()
                : ResidentialBuildings.FirstOrDefault();

            SelectedTargetBuildingForHousehold = prevHouseholdTarget is { } id2
                ? ResidentialBuildings.FirstOrDefault(b => b.Id == id2) ?? ResidentialBuildings.FirstOrDefault()
                : ResidentialBuildings.FirstOrDefault();

            SelectedSettleBuilding = prevSettleTarget is { } id3
                ? ResidentialBuildings.FirstOrDefault(b => b.Id == id3) ?? ResidentialBuildings.FirstOrDefault()
                : ResidentialBuildings.FirstOrDefault();
        }
    }

    private void AddCitizen()
    {
        if (populationService == null)
            return;

        if (string.IsNullOrWhiteSpace(NewCitizenName))
        {
            SetStatus("Введите имя жителя.", isError: true);
            return;
        }

        if (!uint.TryParse(NewCitizenAge, out uint age))
        {
            SetStatus("Возраст должен быть целым неотрицательным числом.", isError: true);
            return;
        }

        var citizen = new Citizen(EntityId.New(), NewCitizenName.Trim(), age, new Position(0, 0))
        {
            Education = SelectedEducationOption.Value
        };

        if (!populationService.RegisterCitizen(citizen))
        {
            SetStatus("Не удалось зарегистрировать жителя.", isError: true);
            return;
        }

        Refresh();
        Citizens.SelectedItem = citizen;
        SetStatus($"Житель «{citizen.Name}» зарегистрирован в городе.", isError: false);
    }

    private void RemoveSelectedCitizen()
    {
        if (populationService == null || Citizens.SelectedItem is not { } citizen)
            return;

        string name = citizen.Name;
        if (populationService.UnregisterCitizen(citizen.Id))
        {
            Refresh();
            SetStatus($"Житель «{name}» удалён из города.", isError: false);
        }
    }

    private void AddCitizenToFamily()
    {
        if (populationService == null || Citizens.SelectedItem is not { } citizen || SelectedTargetFamilyForCitizen is not { } family)
            return;

        if (!populationService.AddMemberToFamily(family.Id, citizen))
        {
            SetStatus($"Не удалось добавить «{citizen.Name}» в семью «{family.Name}» (возможно, уже в семье или не хватает мест в доме).", isError: true);
            return;
        }

        Refresh();
        SetStatus($"Житель «{citizen.Name}» добавлен в семью «{family.Name}».", isError: false);
    }

    private void RemoveCitizenFromFamily()
    {
        if (populationService == null || Citizens.SelectedItem is not { } citizen)
            return;

        var family = FindFamilyOfCitizen(citizen.Id);
        if (family == null)
            return;

        if (populationService.RemoveMemberFromFamily(family.Id, citizen.Id))
        {
            Refresh();
            SetStatus($"Житель «{citizen.Name}» исключён из семьи «{family.Name}».", isError: false);
        }
    }

    private void AddFamily()
    {
        if (populationService == null)
            return;

        if (string.IsNullOrWhiteSpace(NewFamilyName))
        {
            SetStatus("Введите название семьи.", isError: true);
            return;
        }

        var family = new Family(EntityId.New(), NewFamilyName.Trim());
        if (!populationService.RegisterFamily(family))
        {
            SetStatus("Не удалось создать семью.", isError: true);
            return;
        }

        Refresh();
        SelectedFamily = family;
        SetStatus($"Создана семья «{family.Name}».", isError: false);
    }

    private void SettleOrMoveSelectedFamily()
    {
        if (SelectedFamily == null || SelectedTargetBuildingForFamily == null)
            return;

        SettleOrMoveFamily(SelectedFamily, SelectedTargetBuildingForFamily);
    }

    private void SettleFromHouseholdTab()
    {
        if (SelectedSettleFamily == null || SelectedSettleBuilding == null)
            return;

        SettleOrMoveFamily(SelectedSettleFamily, SelectedSettleBuilding);
    }

    private void SettleOrMoveFamily(Family family, BuildingOption buildingOption)
    {
        if (populationService == null)
            return;

        bool alreadySettled = populationService.FindHousehold(family.Id) != null;
        if (alreadySettled)
        {
            if (!populationService.MoveHousehold(family.Id, buildingOption.Id))
            {
                SetStatus($"Не удалось переселить семью «{family.Name}»: не хватает мест или здание недоступно.", isError: true);
                return;
            }

            Refresh();
            SetStatus($"Семья «{family.Name}» переселена.", isError: false);
            return;
        }

        var household = new Household(family, buildingOption.Id, World.Clock.Now);
        if (!populationService.RegisterHousehold(household))
        {
            SetStatus($"Не удалось заселить семью «{family.Name}»: превышена вместимость дома или конфликт проживания.", isError: true);
            return;
        }

        Refresh();
        SelectedHousehold = household;
        SetStatus($"Семья «{family.Name}» заселена.", isError: false);
    }

    private void EvictSelectedFamily()
    {
        if (populationService == null || SelectedFamily == null)
            return;

        if (populationService.UnregisterHousehold(SelectedFamily.Id))
        {
            Refresh();
            SetStatus($"Семья «{SelectedFamily.Name}» выселена из дома.", isError: false);
        }
    }

    private void RemoveSelectedFamily()
    {
        if (populationService == null || SelectedFamily == null)
            return;

        string name = SelectedFamily.Name;
        if (populationService.UnregisterFamily(SelectedFamily.Id))
        {
            Refresh();
            SetStatus($"Семья «{name}» расформирована (жители остались в городе).", isError: false);
        }
    }

    private void MoveSelectedHousehold()
    {
        if (populationService == null || SelectedHousehold == null || SelectedTargetBuildingForHousehold == null)
            return;

        if (!populationService.MoveHousehold(SelectedHousehold.Family.Id, SelectedTargetBuildingForHousehold.Id))
        {
            SetStatus("Не удалось переселить домохозяйство: недостаточно свободных мест.", isError: true);
            return;
        }

        Refresh();
        SetStatus($"Домохозяйство «{SelectedHousehold.Family.Name}» переселено.", isError: false);
    }

    private void EvictSelectedHousehold()
    {
        if (populationService == null || SelectedHousehold == null)
            return;

        string name = SelectedHousehold.Family.Name;
        if (populationService.UnregisterHousehold(SelectedHousehold.Family.Id))
        {
            Refresh();
            SetStatus($"Домохозяйство «{name}» выселено.", isError: false);
        }
    }

    private void RefreshSelectedDetails()
    {
        OnPropertyChanged(nameof(SelectedCitizenName));
        OnPropertyChanged(nameof(SelectedCitizenIdText));
        OnPropertyChanged(nameof(SelectedCitizenAgeText));
        OnPropertyChanged(nameof(SelectedCitizenHealthText));
        OnPropertyChanged(nameof(SelectedCitizenEducationText));
        OnPropertyChanged(nameof(SelectedCitizenPositionText));
        OnPropertyChanged(nameof(SelectedCitizenFamilyText));
        OnPropertyChanged(nameof(SelectedCitizenHomeText));

        OnPropertyChanged(nameof(SelectedFamilyName));
        OnPropertyChanged(nameof(SelectedFamilyMembersCountText));
        OnPropertyChanged(nameof(SelectedFamilyMinRoomsText));
        OnPropertyChanged(nameof(SelectedFamilyHomeText));
        OnPropertyChanged(nameof(SelectedFamilySatisfactionText));
        OnPropertyChanged(nameof(SettleOrMoveFamilyButtonText));

        SelectedFamilyMembers.Clear();
        if (SelectedFamily != null)
        {
            foreach (var member in SelectedFamily.Members)
                SelectedFamilyMembers.Add(member);
        }

        OnPropertyChanged(nameof(SelectedHouseholdFamilyName));
        OnPropertyChanged(nameof(SelectedHouseholdBuildingName));
        OnPropertyChanged(nameof(SelectedHouseholdMoveInDateText));
        OnPropertyChanged(nameof(SelectedHouseholdSatisfactionText));
        OnPropertyChanged(nameof(SelectedHouseholdOccupancyText));

        SelectedHouseholdMembers.Clear();
        if (SelectedHousehold != null)
        {
            foreach (var member in SelectedHousehold.Family.Members)
                SelectedHouseholdMembers.Add(member);
        }
    }

    private Family? FindFamilyOfCitizen(EntityId citizenId)
    {
        return populationService?.Families.FirstOrDefault(f => f.Members.Any(m => m.Id == citizenId));
    }

    private string GetBuildingDisplayName(EntityId buildingId)
    {
        var building = buildingRegistry?.Find(buildingId);
        return building != null ? building.Name : $"Здание [{buildingId}]";
    }

    private string FormatBuildingOccupancy(EntityId buildingId)
    {
        if (populationService == null)
            return "—";

        int residents = populationService.GetResidentsOfBuilding(buildingId).Count;
        var building = buildingRegistry?.Find(buildingId);
        return building != null ? $"{residents} / {building.Capacity} чел." : $"{residents} чел.";
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusBrush = isError
            ? new SolidColorBrush(Color.FromRgb(235, 115, 115))
            : new SolidColorBrush(Color.FromRgb(112, 218, 188));
    }

    private static string FormatEducation(EducationLevel level) => level switch
    {
        EducationLevel.None => "Без образования",
        EducationLevel.School => "Школа",
        EducationLevel.College => "Колледж",
        EducationLevel.University => "Университет",
        _ => level.ToString()
    };
}