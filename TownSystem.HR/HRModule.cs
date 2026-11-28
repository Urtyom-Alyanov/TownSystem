using System.Text.Json.Nodes;

using TownSystem.HR.Models;
using TownSystem.HR.Services;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;
using TownSystem.SharedLib.Core;

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

    private sealed class LazyHRService : IHRService
    {
        private readonly HRModule _owner;
        public LazyHRService(HRModule owner) => _owner = owner;

        private IHRService Real => _owner._service ?? throw new InvalidOperationException("HR не готов");

        public IReadOnlyCollection<IEmployee> Employees => Real.Employees;
        public IReadOnlyCollection<Vacancy> Vacancies => Real.Vacancies;

        public bool OpenVacancy(EntityId departmentId, string position, EducationLevel required, uint count)
            => Real.OpenVacancy(departmentId, position, required, count);

        public bool TryHire(EntityId personId, EntityId departmentId)
            => Real.TryHire(personId, departmentId);

        public bool TryFire(EntityId employeeId)
            => Real.TryFire(employeeId);

        public bool TryTransfer(EntityId employeeId, EntityId toDepartmentId)
            => Real.TryTransfer(employeeId, toDepartmentId);
    }
}