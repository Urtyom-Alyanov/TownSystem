using TownSystem.SharedLib.Core;

namespace TownSystem.SharedLib.Contracts;

public sealed record Vacancy(EntityId DepartmentId, string Position, EducationLevel Required, uint Open) : IEntity
{
    public EntityId Id { get; init; } = EntityId.New();
    public string Name => $"{Position} ({Open} мест)";
}

public interface IEmployee : IEntity
{
    EntityId PersonId { get; }
    EntityId DepartmentId { get; }
    string Position { get; }
    double Salary { get; }
    EducationLevel Qualification { get; }
}

public interface IHRService
{
    IReadOnlyCollection<IEmployee> Employees { get; }
    IReadOnlyCollection<Vacancy> Vacancies { get; }
    bool OpenVacancy(EntityId departmentId, string position, EducationLevel required, uint count);
    bool TryHire(EntityId personId, EntityId departmentId);
    bool TryFire(EntityId employeeId);
    bool TryTransfer(EntityId employeeId, EntityId toDepartmentId);
}