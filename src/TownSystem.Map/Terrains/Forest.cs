namespace TownSystem.Map.Terrains;

/// <summary>
/// лесная клетка, по ней можно передвигаться, с обычной застройкой есть ограничения
/// </summary>
public class Forest : Terrain
{
    public override string Name => "Лес";

    public override bool CanBuild => false;

    public override bool IsPassable => true;
}