using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.Residential;

/// <summary>
/// Жилое здание на карте города.
/// Реализует интерфейс IBuilding для интеграции с общей системой.
/// </summary>
public sealed class ResidentialBuilding : EntityBase, IBuilding
{
    private double wear;
    private readonly List<MaintenanceRecord> maintenanceHistory;

    /// <summary>
    /// Создаёт новое жилое здание.
    /// </summary>
    /// <param name="name">Название здания</param>
    /// <param name="buildingType">Тип жилого здания</param>
    /// <param name="position">Положение на карте</param>
    /// <param name="footprint">Занимаемая площадь</param>
    /// <param name="floors">Количество этажей</param>
    /// <param name="constructionDate">Дата постройки</param>
    /// <param name="id">Уникальный идентификатор (опционально, для загрузки из сохранения)</param>
    public ResidentialBuilding(
        string name,
        ResidentialBuildingType buildingType,
        Position position,
        Footprint footprint,
        uint floors,
        DateTime constructionDate,
        EntityId? id = null)
        : base(name, id)
    {
        if (floors == 0)
            throw new ArgumentException("Количество этажей должно быть больше 0", nameof(floors));

        BuildingType = buildingType;
        Position = position;
        Footprint = footprint;
        Floors = floors;
        ConstructionDate = constructionDate;
        wear = 0.0;
        maintenanceHistory = new List<MaintenanceRecord>();
    }

    /// <summary>
    /// Тип жилого здания.
    /// </summary>
    public ResidentialBuildingType BuildingType { get; }

    /// <summary>
    /// Положение на карте.
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// Размер занимаемой территории в клетках.
    /// </summary>
    public Footprint Footprint { get; }

    /// <summary>
    /// Количество этажей.
    /// </summary>
    public uint Floors { get; }

    /// <summary>
    /// Дата постройки здания.
    /// </summary>
    public DateTime ConstructionDate { get; }

    /// <summary>
    /// История обслуживания здания.
    /// </summary>
    public IReadOnlyList<MaintenanceRecord> MaintenanceHistory => maintenanceHistory.AsReadOnly();

    // IBuilding implementation

    public BuildingCategory Category => BuildingCategory.Residential;

    /// <summary>
    /// Вместимость в жилых единицах (людях).
    /// Расчёт: базовое значение на каждый этаж × количество этажей.
    /// - Частный дом: 4 × 1 = 4
    /// - Малоэтажный (3-4 этажа): 8 × этажи
    /// - Среднеэтажный (5-9 этажей): 12 × этажи
    /// - Многоэтажный (10+ этажей): 15 × этажи
    /// - Общежитие: 20 × этажи
    /// </summary>
    public uint Capacity
    {
        get
        {
            uint baseCapacity = BuildingType switch
            {
                ResidentialBuildingType.PrivateHouse => 4,
                ResidentialBuildingType.LowRiseApartment => 8,
                ResidentialBuildingType.MidRiseApartment => 12,
                ResidentialBuildingType.HighRiseApartment => 15,
                ResidentialBuildingType.Dormitory => 20,
                _ => throw new InvalidOperationException($"Неизвестный тип здания: {BuildingType}")
            };

            return baseCapacity * Floors;
        }
    }

    /// <summary>
    /// Износ здания от 0 (новое) до 1 (полностью разрушено).
    /// </summary>
    public double Wear
    {
        get => wear;
        private set => wear = Math.Clamp(value, 0.0, 1.0);
    }

    /// <summary>
    /// Техническое состояние здания, вычисляемое на основе износа.
    /// </summary>
    public BuildingCondition Condition
    {
        get => Wear switch
        {
            < 0.2 => BuildingCondition.New,
            < 0.4 => BuildingCondition.Good,
            < 0.6 => BuildingCondition.Fair,
            < 0.8 => BuildingCondition.NeedsRepair,
            < 0.95 => BuildingCondition.Poor,
            _ => BuildingCondition.Critical
        };
    }

    /// <summary>
    /// Возраст здания в годах.
    /// </summary>
    public int AgeInYears
    {
        get => DateTime.Now.Year - ConstructionDate.Year;
    }

    /// <summary>
    /// Увеличивает износ здания из-за времени и использования.
    /// Скорость износа зависит от возраста здания.
    /// </summary>
    /// <param name="timeDelta">Прошедшее игровое время</param>
    public void AccumulateWear(TimeSpan timeDelta)
    {
        // Износ увеличивается с течением времени
        // Формула: 0.00001 за каждый день игрового времени (из расчёта на 100 лет полного износа)
        double dailyWearRate = 0.00001;
        double wearIncrease = dailyWearRate * timeDelta.TotalDays;

        Wear += wearIncrease;
    }

    /// <summary>
    /// Проводит обслуживание здания, снижая его износ.
    /// </summary>
    /// <param name="maintenanceType">Тип обслуживания</param>
    /// <param name="cost">Стоимость обслуживания</param>
    /// <param name="date">Дата проведения (опционально, по умолчанию DateTime.Now)</param>
    /// <param name="description">Описание работ (опционально)</param>
    /// <returns>Насколько был снижен износ</returns>
    public double Maintain(MaintenanceType maintenanceType, double cost, DateTime? date = null, string? description = null)
    {
        double wearReduction = maintenanceType switch
        {
            MaintenanceType.Inspection => 0.0,          // Осмотр не снижает износ
            MaintenanceType.Repair => 0.05,             // Текущий ремонт: -5%
            MaintenanceType.MajorRepair => 0.3,         // Капитальный: -30%
            MaintenanceType.Emergency => 0.1,           // Экстренный: -10%
            _ => 0.0
        };

        double oldWear = Wear;
        Wear -= wearReduction;

        var record = new MaintenanceRecord(
            date ?? DateTime.Now,
            maintenanceType,
            cost,
            wearReduction
        )
        {
            Description = description
        };
        maintenanceHistory.Add(record);

        return oldWear - Wear;
    }

    /// <summary>
    /// Восстанавливает износ и историю обслуживания при загрузке сохранения без повторного применения эффектов ремонта.
    /// </summary>
    internal void RestoreState(double savedWear, IEnumerable<MaintenanceRecord> savedHistory)
    {
        ArgumentNullException.ThrowIfNull(savedHistory);
        if (!double.IsFinite(savedWear))
            throw new ArgumentOutOfRangeException(nameof(savedWear), "Износ должен быть конечным числом");

        Wear = savedWear;
        maintenanceHistory.Clear();
        maintenanceHistory.AddRange(savedHistory);
    }

    /// <summary>
    /// Получает информацию о времени последнего обслуживания.
    /// </summary>
    /// <returns>Дата последнего обслуживания или null, если обслуживания не было</returns>
    public DateTime? LastMaintenanceDate
    {
        get => maintenanceHistory.Count > 0 ? maintenanceHistory[^1].Date : null;
    }

    /// <summary>
    /// Возвращает общую стоимость всех проведённых работ.
    /// </summary>
    public double TotalMaintenanceCost
    {
        get => maintenanceHistory.Sum(r => r.Cost);
    }
}
