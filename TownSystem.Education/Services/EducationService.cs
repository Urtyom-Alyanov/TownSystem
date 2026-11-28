using TownSystem.Education.Models;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.Education.Services;

public sealed class EducationService : IEducationService
{
    private readonly List<EducationBuilding> _buildings = new();
    private readonly List<Enrollment> _enrollments = new();
    private readonly IEventBus _events;
    private readonly IPopulationService _people;

    public EducationService(IEventBus events, IPopulationService people)
    {
        _events = events;
        _people = people;
    }

    public IReadOnlyCollection<IEducationBuilding> Buildings => _buildings;
    public IReadOnlyCollection<Enrollment> Enrollments => _enrollments;
    public IReadOnlyCollection<EducationBuilding> ConcreteBuildings => _buildings;

    public void AddBuilding(EducationBuilding building) => _buildings.Add(building);

    public bool TryEnroll(EntityId personId, EntityId buildingId)
    {
        var person = _people.Find(personId);
        var building = _buildings.FirstOrDefault(b => b.Id == buildingId);
        if (person == null || building == null || !building.HasSpace)
            return false;

        var required = building.Level switch
        {
            SchoolLevel.School => EducationLevel.None,
            SchoolLevel.College => EducationLevel.School,
            SchoolLevel.University => EducationLevel.College,
            _ => EducationLevel.University
        };
        if (person.Education < required || person.Education >= ToLevel(building.Level))
            return false;

        building.Enrolled++;
        person.WorkplaceId = buildingId;
        _enrollments.Add(new Enrollment(personId, buildingId, building.Level, DateTime.UtcNow));
        _events.Publish(new StudentEnrolled(personId, buildingId));
        return true;
    }

    public bool TryRelease(EntityId personId)
    {
        var index = _enrollments.FindIndex(e => e.StudentId == personId);
        if (index < 0) return false;
        var enrollment = _enrollments[index];
        var building = _buildings.FirstOrDefault(b => b.Id == enrollment.BuildingId);
        if (building != null) building.Enrolled = Math.Max(0, building.Enrolled - 1);

        var person = _people.Find(personId);
        if (person != null)
        {
            var achieved = ToLevel(enrollment.Level);
            if (person.Education < achieved) person.Education = achieved;
            person.WorkplaceId = null;
            _events.Publish(new StudentGraduated(personId, enrollment.Level, achieved));
        }
        _enrollments.RemoveAt(index);
        return true;
    }

    public void Advance(TimeSpan delta) { }

    private static EducationLevel ToLevel(SchoolLevel level) => level switch
    {
        SchoolLevel.School => EducationLevel.School,
        SchoolLevel.College => EducationLevel.College,
        SchoolLevel.University => EducationLevel.University,
        _ => EducationLevel.None
    };
}

public sealed record StudentEnrolled(EntityId PersonId, EntityId BuildingId) : IWorldEvent;
public sealed record StudentGraduated(EntityId PersonId, SchoolLevel Level, EducationLevel Achieved) : IWorldEvent;