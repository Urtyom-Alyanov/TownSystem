using TownSystem.SharedLib.Core;

namespace TownSystem.SharedLib.Contracts;

public enum SchoolLevel { School, College, University }

public interface IEducationBuilding : IBuilding
{
    SchoolLevel Level { get; }
    uint Enrolled { get; }
    uint Teachers { get; }
}

public sealed record Enrollment(EntityId StudentId, EntityId BuildingId, SchoolLevel Level, DateTime StartedAt) : IEntity
{
    // Генерируем уникальный ID для самого факта зачисления
    public EntityId Id { get; init; } = EntityId.New();

    // В списке будет показываться текст вместо имени
    public string Name => $"Студент {StudentId.ToString()[..4]}...";
}

public interface IEducationService
{
    IReadOnlyCollection<IEducationBuilding> Buildings { get; }
    IReadOnlyCollection<Enrollment> Enrollments { get; }
    bool TryEnroll(EntityId personId, EntityId buildingId);
    bool TryRelease(EntityId personId);
    void Advance(TimeSpan delta);
}