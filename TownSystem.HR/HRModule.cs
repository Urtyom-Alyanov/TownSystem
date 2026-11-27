using System.Text.Json.Nodes;

using TownSystem.HR.Models;
using TownSystem.HR.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.HR;

public sealed class HRModule : ISimulationModule, ISaveable
{
    private IWorld _world = null!;
    private HRService _service = null!;

    public string Name => "HR";
    public int Order => 0;
    public string SaveKey => "hr";

    public void Initialize(IWorld world)
    {
        _world = world;
        world.RegisterService<IHRService>(new LazyHRService(this));
    }

    public void OnWorldReady()
    {
        _service = new HRService(_world.GetService<IPopulationService>(), _world.Events);
        var dept = new Department("Завод №1");
        _service.AddDepartment(dept);
        _service.OpenVacancy(dept.Id, "Инженер", EducationLevel.College, 5);
    }

    public void Tick(TickContext context) { }

    public JsonNode Save()
    {
        var arr = new JsonArray();
        if (_service != null)
            foreach (var e in _service.Employees)
                arr.Add(new JsonObject
                {
                    ["id"] = e.Id.Value.ToString(),
                    ["name"] = e.Name,
                    ["person"] = e.PersonId.Value.ToString(),
                    ["dept"] = e.DepartmentId.Value.ToString(),
                    ["pos"] = e.Position,
                    ["salary"] = e.Salary
                });
        return new JsonObject { ["employees"] = arr };
    }

    public void Load(JsonNode state) { }

    private sealed class LazyHRService(HRModule owner) : IHRService
    {
        private IHRService Real => owner._service ?? throw new InvalidOperationException("HR не готов");
        public IReadOnlyCollection<IEmployee> Employees => Real.Employees;
        public IReadOnlyCollection<Vacancy> Vacancies => Real.Vacancies;
        public bool OpenVacancy(EntityId d, string p, EducationLevel q, uint c) => Real.OpenVacancy(d, p, q, c);
        public bool TryHire(EntityId p, EntityId d) => Real.TryHire(p, d);
        public bool TryFire(EntityId e) => Real.TryFire(e);
        public bool TryTransfer(EntityId e, EntityId d) => Real.TryTransfer(e, d);
    }
}