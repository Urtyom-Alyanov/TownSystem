using System;
using System.Collections.Generic;
using System.Linq;

using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.UIApp;

public class FakePerson : IPerson
{
    public EntityId Id { get; } = EntityId.New();
    public string Name { get; set; } = "Житель";
    public uint Age { get; set; } = 20;
    public Position Position { get; set; } = new(0, 0);
    public EntityId? HomeId { get; set; }
    public EntityId? WorkplaceId { get; set; }
    public EducationLevel Education { get; set; } = EducationLevel.None;
    public double Health { get; set; } = 1.0;
}

public class FakePopulationService : IPopulationService
{
    private readonly List<IPerson> _people = new();

    public IReadOnlyCollection<IPerson> People => _people;

    public FakePopulationService()
    {
        // Добавляем пару тестовых жителей для проверки
        _people.Add(new FakePerson { Name = "Иван (без образования)", Education = EducationLevel.None });
        _people.Add(new FakePerson { Name = "Мария (есть школа)", Education = EducationLevel.School });
        _people.Add(new FakePerson { Name = "Петр (есть колледж)", Education = EducationLevel.College });
    }

    public IPerson? Find(EntityId id) => _people.FirstOrDefault(p => p.Id == id);
}