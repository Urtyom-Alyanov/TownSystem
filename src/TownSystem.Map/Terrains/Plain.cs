namespace TownSystem.Map.Terrains;

/// <summary>
/// обычная равнина, тут можно передвигаться и ставить здания
/// </summary>
public class Plain : Terrain
{
    public override string Name => "Равнина";

    public override bool CanBuild => true;

    public override bool IsPassable => true;
}