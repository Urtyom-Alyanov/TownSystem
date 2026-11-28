using NUnit.Framework;

using TownSystem.HR.Models;
using TownSystem.HR.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.HR.Tests;

public class HRTests
{
    private class FakePerson : IPerson
    {
        public EntityId Id { get; } = EntityId.New();
        public string Name => "Тестовый Работник";
        public uint Age { get; set; } = 25;
        public Position Position { get; set; } = new(0, 0);
        public EntityId? HomeId { get; set; }
        public EntityId? WorkplaceId { get; set; }
        public EducationLevel Education { get; set; } = EducationLevel.College;
        public double Health { get; set; } = 1.0;
    }

    private class FakePeople : IPopulationService
    {
        public List<IPerson> People { get; } = new();
        IReadOnlyCollection<IPerson> IPopulationService.People => People;
        public IPerson? Find(EntityId id) => People.FirstOrDefault(p => p.Id == id);
    }

    [Test]
    public void Should_Hire_When_Qualification_Matches()
    {
        var people = new FakePeople();
        var worker = new FakePerson { Education = EducationLevel.College };
        people.People.Add(worker);

        var service = new HRService(people, new EventBus());
        var dept = new Department("Завод");
        service.AddDepartment(dept);
        service.OpenVacancy(dept.Id, "Инженер", EducationLevel.School, 5); // Требуется школа, у кандидата колледж

        bool result = service.TryHire(worker.Id, dept.Id);

        Assert.That(result, Is.True);
        Assert.That(service.Employees.Count, Is.EqualTo(1));
        Assert.That(worker.WorkplaceId, Is.EqualTo(dept.Id));
        Assert.That(service.Vacancies.First().Open, Is.EqualTo(4)); // Вакансий стало на 1 меньше
    }

    [Test]
    public void Should_Not_Hire_When_Qualification_Is_Low()
    {
        var people = new FakePeople();
        var worker = new FakePerson { Education = EducationLevel.None };
        people.People.Add(worker);

        var service = new HRService(people, new EventBus());
        var dept = new Department("Завод");
        service.AddDepartment(dept);
        service.OpenVacancy(dept.Id, "Инженер", EducationLevel.University, 5); // Требуется ВУЗ

        bool result = service.TryHire(worker.Id, dept.Id);

        Assert.That(result, Is.False);
        Assert.That(service.Employees.Count, Is.EqualTo(0));
        Assert.That(worker.WorkplaceId, Is.Null);
    }

    [Test]
    public void Should_Fire_Employee_And_Free_Workplace()
    {
        var people = new FakePeople();
        var worker = new FakePerson { Education = EducationLevel.School };
        people.People.Add(worker);

        var service = new HRService(people, new EventBus());
        var dept = new Department("Цех");
        service.AddDepartment(dept);
        service.OpenVacancy(dept.Id, "Слесарь", EducationLevel.School, 1);
        service.TryHire(worker.Id, dept.Id);

        var employee = service.Employees.First();

        bool result = service.TryFire(employee.Id);

        Assert.That(result, Is.True);
        Assert.That(service.Employees.Count, Is.EqualTo(0)); // Уволен
        Assert.That(worker.WorkplaceId, Is.Null); // Больше нигде не работает
    }

    [Test]
    public void Should_Transfer_Employee_To_Another_Department()
    {
        var people = new FakePeople();
        var worker = new FakePerson { Education = EducationLevel.School };
        people.People.Add(worker);

        var service = new HRService(people, new EventBus());
        var deptA = new Department("Отдел А");
        var deptB = new Department("Отдел Б");
        service.AddDepartment(deptA);
        service.AddDepartment(deptB);

        service.OpenVacancy(deptA.Id, "Менеджер", EducationLevel.School, 1);
        service.TryHire(worker.Id, deptA.Id);
        var employee = service.Employees.First();

        bool result = service.TryTransfer(employee.Id, deptB.Id);

        Assert.That(result, Is.True);
        Assert.That(employee.DepartmentId, Is.EqualTo(deptB.Id)); // Изменился в карточке сотрудника
        Assert.That(worker.WorkplaceId, Is.EqualTo(deptB.Id)); // Изменился в профиле жителя
    }
}