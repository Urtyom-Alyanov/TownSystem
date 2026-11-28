using System.Text.Json.Nodes;

using TownSystem.Education.Models;
using TownSystem.Education.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.Education;

public sealed class EducationModule : ISimulationModule, ISaveable
{
    private IWorld _world = null!;
    private EducationService _service = null!;

    public string Name => "Education";
    public int Order => 0;
    public string SaveKey => "education";

    public void Initialize(IWorld world)
    {
        _world = world;
        world.RegisterService<IEducationService>(new LazyEducationService(this));
    }

    public void OnWorldReady()
    {
        var people = _world.GetService<IPopulationService>();
        _service = new EducationService(_world.Events, people);
        _service.AddBuilding(new EducationBuilding("Школа №1", SchoolLevel.School,
            new Position(10, 10), new Footprint(2, 2), capacity: 300, floors: 3));
    }

    public void Tick(TickContext context) => _service?.Advance(context.Delta);

    public JsonNode Save()
    {
        var array = new JsonArray();
        if (_service != null)
            foreach (var b in _service.ConcreteBuildings)
                array.Add(new JsonObject
                {
                    ["id"] = b.Id.Value.ToString(),
                    ["name"] = b.Name,
                    ["level"] = b.Level.ToString(),
                    ["enrolled"] = b.Enrolled
                });
        return new JsonObject { ["buildings"] = array };
    }

    public void Load(JsonNode state) { }

    private sealed class LazyEducationService(EducationModule owner) : IEducationService
    {
        private IEducationService Real => owner._service ?? throw new InvalidOperationException("Образование ещё не готово");
        public IReadOnlyCollection<IEducationBuilding> Buildings => Real.Buildings;
        public IReadOnlyCollection<Enrollment> Enrollments => Real.Enrollments;
        public bool TryEnroll(EntityId p, EntityId b) => Real.TryEnroll(p, b);
        public bool TryRelease(EntityId p) => Real.TryRelease(p);
        public void Advance(TimeSpan d) => Real.Advance(d);
    }
}