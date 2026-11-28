using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.HR.Models;

public sealed class Employee : EntityBase, IEmployee
{
    public Employee(string name, EntityId personId, EntityId departmentId, string position,
        double salary, EducationLevel qualification, EntityId? id = null) : base(name, id)
    {
        PersonId = personId; DepartmentId = departmentId; Position = position;
        Salary = salary; Qualification = qualification;
    }

    public EntityId PersonId { get; }
    public EntityId DepartmentId { get; internal set; }
    public string Position { get; internal set; }
    public double Salary { get; internal set; }
    public EducationLevel Qualification { get; internal set; }
}