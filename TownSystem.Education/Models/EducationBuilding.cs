using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.Education.Models;

public sealed class EducationBuilding : EntityBase, IEducationBuilding
{
    public EducationBuilding(string name, SchoolLevel level, Position position, Footprint footprint,
        uint capacity, uint floors, EntityId? id = null) : base(name, id)
    {
        Level = level; Position = position; Footprint = footprint;
        Capacity = capacity; Floors = floors;
    }

    public BuildingCategory Category => BuildingCategory.Education;
    public SchoolLevel Level { get; }
    public Position Position { get; }
    public Footprint Footprint { get; }
    public uint Floors { get; }
    public uint Capacity { get; }
    public uint Enrolled { get; internal set; }
    public uint Teachers { get; internal set; }
    public double Wear { get; internal set; }

    public bool HasSpace => Enrolled < Capacity;
    public double Load => Capacity == 0 ? 0 : (double)Enrolled / Capacity;
}