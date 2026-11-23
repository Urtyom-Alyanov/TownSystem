using TownSystem.SharedLib.Core;

namespace TownSystem.Population;

/// <summary>
/// Домохозяйство - семья, связанная с определённым жилым зданием.
/// </summary>
public class Household
{
    private readonly Family family;
    private double satisfaction = 0.5;

    /// <summary>
    /// Создаёт домохозяйство.
    /// </summary>
    /// <param name="family">Семья</param>
    /// <param name="buildingId">ID здания, где проживает семья</param>
    /// <param name="moveInDate">Дата заселения (опционально, по умолчанию DateTime.UtcNow)</param>
    public Household(Family family, EntityId buildingId, DateTime? moveInDate = null)
    {
        this.family = family ?? throw new ArgumentNullException(nameof(family));
        if (buildingId == default)
            throw new ArgumentException("Идентификатор здания не может быть пустым", nameof(buildingId));

        BuildingId = buildingId;
        MoveInDate = moveInDate ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Семья в домохозяйстве.
    /// </summary>
    public Family Family => family;

    /// <summary>
    /// ID здания, где живёт семья. Изменяется через PopulationService.
    /// </summary>
    public EntityId BuildingId { get; internal set; }

    /// <summary>
    /// Дата заселения в текущее здание.
    /// </summary>
    public DateTime MoveInDate { get; }

    /// <summary>
    /// Количество игровых дней, прожитых в текущем здании.
    /// </summary>
    public int DaysInBuilding => (int)((DateTime.UtcNow - MoveInDate).TotalDays);

    /// <summary>
    /// Среднее удовлетворение домохозяйства от жилья.
    /// Зависит от состояния здания, размера жилья, удобств.
    /// От 0 (недовольны) до 1 (полностью довольны).
    /// </summary>
    public double Satisfaction
    {
        get => satisfaction;
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), "Удовлетворённость должна быть конечным числом");

            satisfaction = Math.Clamp(value, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Возвращает строковое представление для отладки.
    /// </summary>
    public override string ToString() =>
        $"Household({Family.Name}, building={BuildingId}, satisfaction={Satisfaction:0.00})";
}