namespace TownSystem.Map.Terrains;

/// <summary>
/// Горный участок, обычные постройки и проезд по нему закрыты
/// </summary>
public class Mountain : Terrain
{
    public override string Name => "Горы";

    public override bool CanBuild => false;

    public override bool IsPassable => false;
}
