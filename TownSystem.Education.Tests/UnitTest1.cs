using NUnit.Framework;

using TownSystem.Education.Models;
using TownSystem.Education.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.Education.Tests;

public class EducationTests
{
    // Вспомогательный класс, имитирующий жителя города
    private class FakePerson : IPerson
    {
        public EntityId Id { get; } = EntityId.New();
        public string Name => "Тестовый Студент";
        public uint Age { get; set; } = 10;
        public Position Position { get; set; } = new(0, 0);
        public EntityId? HomeId { get; set; }
        public EntityId? WorkplaceId { get; set; }
        public EducationLevel Education { get; set; } = EducationLevel.None;
        public double Health { get; set; } = 1.0;
    }

    // Вспомогательный сервис населения
    private class FakePeople : IPopulationService
    {
        public List<IPerson> People { get; } = new();
        IReadOnlyCollection<IPerson> IPopulationService.People => People;
        public IPerson? Find(EntityId id) => People.FirstOrDefault(p => p.Id == id);
    }

    [Test]
    public void Should_Enroll_When_Capacity_And_Education_Match()
    {
        var people = new FakePeople();
        var student = new FakePerson { Education = EducationLevel.None };
        people.People.Add(student);

        var service = new EducationService(new EventBus(), people);
        var school = new EducationBuilding("Школа №1", SchoolLevel.School, new Position(0, 0), new Footprint(1, 1), 10, 2);
        service.AddBuilding(school);

        bool result = service.TryEnroll(student.Id, school.Id);

        Assert.That(result, Is.True);
        Assert.That(school.Enrolled, Is.EqualTo(1));
        Assert.That(student.WorkplaceId, Is.EqualTo(school.Id));
    }

    [Test]
    public void Should_Not_Enroll_When_No_Capacity()
    {
        var people = new FakePeople();
        var student1 = new FakePerson();
        var student2 = new FakePerson();
        people.People.Add(student1);
        people.People.Add(student2);

        var service = new EducationService(new EventBus(), people);
        var school = new EducationBuilding("Маленькая школа", SchoolLevel.School, new Position(0, 0), new Footprint(1, 1), 1, 1);
        service.AddBuilding(school);

        service.TryEnroll(student1.Id, school.Id); // Занимаем единственное место
        bool result = service.TryEnroll(student2.Id, school.Id); // Пытаемся посадить второго

        Assert.That(result, Is.False);
        Assert.That(school.Enrolled, Is.EqualTo(1));
        Assert.That(student2.WorkplaceId, Is.Null);
    }

    [Test]
    public void Should_Not_Enroll_If_Education_Level_Is_Too_Low()
    {
        var people = new FakePeople();
        var student = new FakePerson { Education = EducationLevel.None }; // Только школа
        people.People.Add(student);

        var service = new EducationService(new EventBus(), people);
        var university = new EducationBuilding("ВУЗ", SchoolLevel.University, new Position(0, 0), new Footprint(1, 1), 100, 5);
        service.AddBuilding(university);

        bool result = service.TryEnroll(student.Id, university.Id);

        Assert.That(result, Is.False); // В ВУЗ нельзя без колледжа
    }

    [Test]
    public void Should_Graduate_And_Upgrade_Education_Level()
    {
        var people = new FakePeople();
        var student = new FakePerson { Education = EducationLevel.None };
        people.People.Add(student);

        var service = new EducationService(new EventBus(), people);
        var school = new EducationBuilding("Школа", SchoolLevel.School, new Position(0, 0), new Footprint(1, 1), 10, 1);
        service.AddBuilding(school);
        service.TryEnroll(student.Id, school.Id);

        bool result = service.TryRelease(student.Id);

        Assert.That(result, Is.True);
        Assert.That(school.Enrolled, Is.EqualTo(0)); // Место освободилось
        Assert.That(student.Education, Is.EqualTo(EducationLevel.School)); // Получил образование
        Assert.That(student.WorkplaceId, Is.Null); // Больше не числится в школе
    }
}