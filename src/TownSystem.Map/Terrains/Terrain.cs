namespace TownSystem.Map.Terrains;

/// <summary>
/// Общие свойства местности, по ним понятно можно ли тут строить и передвигаться
/// </summary>
public abstract class Terrain
{
    public abstract string Name { get; }

    public abstract bool CanBuild { get; }

    // по некоторым клеткам можно пройти даже если строить на них нельзя
    public abstract bool IsPassable { get; }
}
