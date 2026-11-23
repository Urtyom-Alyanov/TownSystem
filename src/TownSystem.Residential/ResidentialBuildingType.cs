namespace TownSystem.Residential;

/// <summary>
/// Тип жилого здания.
/// </summary>
public enum ResidentialBuildingType
{
    /// <summary>Частный дом.</summary>
    PrivateHouse,

    /// <summary>Малоэтажный жилой дом (2-4 этажа).</summary>
    LowRiseApartment,

    /// <summary>Среднеэтажный жилой дом (5-9 этажей).</summary>
    MidRiseApartment,

    /// <summary>Многоэтажный жилой дом (10+ этажей).</summary>
    HighRiseApartment,

    /// <summary>Общежитие для студентов и рабочих.</summary>
    Dormitory
}
