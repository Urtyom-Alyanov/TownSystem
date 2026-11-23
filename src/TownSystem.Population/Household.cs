using TownSystem.SharedLib.Core;

namespace TownSystem.Population;

/// <summary>
/// Домохозяйство - семья, связанная с определённым жилым зданием.
/// </summary>
public class Household
{
    private readonly Family family;

    /// <summary>
    /// Создаёт домохозяйство.
    /// </summary>
    /// <param name="family">Семья</param>
    /// <param name="buildingId">ID здания, где проживает семья</param>
    public Household(Family family, EntityId buildingId)
    {
        this.family = family ?? throw new ArgumentNullException(nameof(family));
        BuildingId = buildingId;
        MoveInDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Семья в домохозяйстве.
    /// </summary>
    public Family Family => family;

    /// <summary>
    /// ID здания, где живёт семья.
    /// </summary>
    public EntityId BuildingId { get; set; }

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
    public double Satisfaction { get; set; } = 0.5;

    /// <summary>
    /// Возвращает строковое представление для отладки.
    /// </summary>
    public override string ToString() => 
        $"Household({Family.Name}, building={BuildingId}, satisfaction={Satisfaction:0.00})";
}
