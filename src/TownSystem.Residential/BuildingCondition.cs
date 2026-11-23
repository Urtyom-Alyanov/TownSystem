namespace TownSystem.Residential;

/// <summary>
/// Техническое состояние здания, зависящее от износа.
/// </summary>
public enum BuildingCondition
{
    /// <summary>Новое здание (износ &lt; 0.2).</summary>
    New,

    /// <summary>Хорошее состояние (износ 0.2-0.4).</summary>
    Good,

    /// <summary>Удовлетворительное состояние (износ 0.4-0.6).</summary>
    Fair,

    /// <summary>Требует ремонта (износ 0.6-0.8).</summary>
    NeedsRepair,

    /// <summary>Плохое состояние (износ 0.8-0.95).</summary>
    Poor,

    /// <summary>Критическое состояние (износ &gt;= 0.95).</summary>
    Critical
}
