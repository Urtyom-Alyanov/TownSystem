namespace TownSystem.Residential;

/// <summary>
/// Тип проведённого обслуживания.
/// </summary>
public enum MaintenanceType
{
    /// <summary>Плановая проверка состояния.</summary>
    Inspection,

    /// <summary>Текущий ремонт.</summary>
    Repair,

    /// <summary>Капитальный ремонт.</summary>
    MajorRepair,

    /// <summary>Экстренное вмешательство.</summary>
    Emergency
}

/// <summary>
/// Запись об обслуживании жилого здания.
/// </summary>
public sealed record MaintenanceRecord(
    DateTime Date,
    MaintenanceType Type,
    double Cost,
    double WearReduction
)
{
    /// <summary>Описание работ (опционально).</summary>
    public string? Description { get; init; }
}
