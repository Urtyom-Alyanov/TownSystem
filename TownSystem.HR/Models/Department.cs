using TownSystem.SharedLib.Core;

namespace TownSystem.HR.Models;

public sealed class Department : EntityBase
{
    public Department(string name, EntityId? parentId = null, EntityId? id = null) : base(name, id)
        => ParentId = parentId;
    public EntityId? ParentId { get; }
}