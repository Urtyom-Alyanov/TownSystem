using TownSystem.HR.Models;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.HR.Services;

public sealed class HRService : IHRService
{
    private readonly List<Department> _departments = new();
    private readonly List<Employee> _employees = new();
    private readonly List<Vacancy> _vacancies = new();
    private readonly IPopulationService _people;
    private readonly IEventBus _events;

    public HRService(IPopulationService people, IEventBus events)
    {
        _people = people; _events = events;
    }

    public IReadOnlyCollection<IEmployee> Employees => _employees;
    public IReadOnlyCollection<Vacancy> Vacancies => _vacancies;
    public IReadOnlyCollection<Department> Departments => _departments;

    public void AddDepartment(Department d) => _departments.Add(d);

    public bool OpenVacancy(EntityId departmentId, string position, EducationLevel required, uint count)
    {
        if (!_departments.Any(d => d.Id == departmentId) || count == 0) return false;
        _vacancies.Add(new Vacancy(departmentId, position, required, count));
        _events.Publish(new VacancyOpened(departmentId, position));
        return true;
    }

    public bool TryHire(EntityId personId, EntityId departmentId)
    {
        var person = _people.Find(personId);
        if (person == null || _employees.Any(e => e.PersonId == personId)) return false;
        var vacancy = _vacancies.FirstOrDefault(v => v.DepartmentId == departmentId && v.Required <= person.Education);
        if (vacancy == null || vacancy.Open == 0) return false;

        var employee = new Employee(person.Name, personId, departmentId, vacancy.Position,
            salary: 1000, person.Education);
        _employees.Add(employee);
        person.WorkplaceId = departmentId;
        DecreaseVacancy(vacancy);
        _events.Publish(new EmployeeHired(personId, employee.Id, departmentId));
        return true;
    }

    public bool TryFire(EntityId employeeId)
    {
        var emp = _employees.FirstOrDefault(e => e.Id == employeeId);
        if (emp == null) return false;
        _employees.Remove(emp);
        var person = _people.Find(emp.PersonId);
        if (person != null) person.WorkplaceId = null;
        _events.Publish(new EmployeeFired(emp.PersonId, emp.Id, emp.DepartmentId));
        return true;
    }

    public bool TryTransfer(EntityId employeeId, EntityId toDepartmentId)
    {
        var emp = _employees.FirstOrDefault(e => e.Id == employeeId);
        if (emp == null || !_departments.Any(d => d.Id == toDepartmentId)) return false;
        emp.DepartmentId = toDepartmentId;
        var person = _people.Find(emp.PersonId);
        if (person != null) person.WorkplaceId = toDepartmentId;
        _events.Publish(new EmployeeTransferred(emp.Id, toDepartmentId));
        return true;
    }

    private void DecreaseVacancy(Vacancy v)
    {
        var i = _vacancies.IndexOf(v);
        _vacancies[i] = v with { Open = v.Open - 1 };
    }
}

public sealed record VacancyOpened(EntityId DepartmentId, string Position) : IWorldEvent;
public sealed record EmployeeHired(EntityId PersonId, EntityId EmployeeId, EntityId DepartmentId) : IWorldEvent;
public sealed record EmployeeFired(EntityId PersonId, EntityId EmployeeId, EntityId DepartmentId) : IWorldEvent;
public sealed record EmployeeTransferred(EntityId EmployeeId, EntityId DepartmentId) : IWorldEvent;