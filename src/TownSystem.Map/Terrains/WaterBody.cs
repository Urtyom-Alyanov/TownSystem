namespace TownSystem.Map.Terrains;

/// <summary>
/// клетка с рекой или водоёмом, обычные здания тут не ставим
/// </summary>
public class WaterBody : Terrain
{
    public bool IsRiver { get; }

    // Так создаём воду для моря или озера
    public WaterBody() : this(false)
    {
    }

    // передаём признак реки, чтоб в карточке было видно что это река
    internal WaterBody(bool isRiver)
    {
        IsRiver = isRiver;
    }

    public override string Name => IsRiver ? "Река" : "Водоём";
    public override bool CanBuild => false;
    public override bool IsPassable => false;
}
