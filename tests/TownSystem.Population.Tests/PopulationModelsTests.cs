using TownSystem.Population;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Contracts;

namespace TownSystem.Population.Tests;

/// <summary>
/// Тесты для моделей Citizen, Family, Household и PopulationService.
/// </summary>
public class PopulationModelsTests
{
    #region Citizen Tests

    [Fact]
    public void Citizen_Constructor_InitializesProperties()
    {
        var id = EntityId.New();
        var name = "Иван Петров";
        var age = 30u;
        var position = new Position(10.5, 20.3);

        var citizen = new Citizen(id, name, age, position);

        Assert.Equal(id, citizen.Id);
        Assert.Equal(name, citizen.Name);
        Assert.Equal(age, citizen.Age);
        Assert.Equal(position, citizen.Position);
        Assert.Null(citizen.HomeId);
        Assert.Null(citizen.WorkplaceId);
        Assert.Equal(EducationLevel.None, citizen.Education);
        Assert.Equal(1.0, citizen.Health);
    }

    [Fact]
    public void Citizen_ImplementsIPerson()
    {
        var citizen = new Citizen(EntityId.New(), "Тест", 25, new Position(0, 0));
        Assert.IsAssignableFrom<IPerson>(citizen);
    }

    [Fact]
    public void Citizen_CanSetProperties()
    {
        var citizen = new Citizen(EntityId.New(), "Тест", 25, new Position(0, 0));
        var newPosition = new Position(5.0, 10.0);
        var workplaceId = EntityId.New();

        citizen.Position = newPosition;
        citizen.WorkplaceId = workplaceId;
        citizen.Education = EducationLevel.College;
        citizen.Health = 0.75;
        citizen.Age = 26;

        Assert.Equal(newPosition, citizen.Position);
        Assert.Null(citizen.HomeId);
        Assert.Equal(workplaceId, citizen.WorkplaceId);
        Assert.Equal(EducationLevel.College, citizen.Education);
        Assert.Equal(0.75, citizen.Health);
        Assert.Equal(26u, citizen.Age);
    }

    [Fact]
    public void Citizen_ToString_ContainsNameAndStats()
    {
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        citizen.Health = 0.85;

        var str = citizen.ToString();

        Assert.Contains("Иван", str);
        Assert.Contains("30", str);
        // ToString может использовать локаль, поэтому проверим оба варианта
        Assert.True(str.Contains("0.85") || str.Contains("0,85"), $"String: {str}");
    }

    #endregion

    #region Family Tests

    [Fact]
    public void Family_Constructor_InitializesProperties()
    {
        var id = EntityId.New();
        var name = "Петровы";

        var family = new Family(id, name);

        Assert.Equal(id, family.Id);
        Assert.Equal(name, family.Name);
        Assert.Empty(family.Members);
        Assert.Equal(0, family.MemberCount);
    }

    [Fact]
    public void Family_AddMember_IncreasesCount()
    {
        var family = new Family(EntityId.New(), "Семья");
        var citizen = new Citizen(EntityId.New(), "Член", 25, new Position(0, 0));

        family.AddMember(citizen);

        Assert.Single(family.Members);
        Assert.Equal(1, family.MemberCount);
    }

    [Fact]
    public void Family_AddMember_NullThrows()
    {
        var family = new Family(EntityId.New(), "Семья");

        Assert.Throws<ArgumentNullException>(() => family.AddMember(null!));
    }

    [Fact]
    public void Family_AddMember_DuplicateNotAdded()
    {
        var family = new Family(EntityId.New(), "Семья");
        var citizen = new Citizen(EntityId.New(), "Член", 25, new Position(0, 0));

        family.AddMember(citizen);
        family.AddMember(citizen);

        Assert.Single(family.Members);
        Assert.Equal(1, family.MemberCount);
    }

    [Fact]
    public void Family_RemoveMember_DecreaseCount()
    {
        var family = new Family(EntityId.New(), "Семья");
        var citizen = new Citizen(EntityId.New(), "Член", 25, new Position(0, 0));

        family.AddMember(citizen);
        Assert.Equal(1, family.MemberCount);

        family.RemoveMember(citizen);

        Assert.Empty(family.Members);
        Assert.Equal(0, family.MemberCount);
    }

    [Fact]
    public void Family_HasMember_ReturnsCorrectValue()
    {
        var family = new Family(EntityId.New(), "Семья");
        var member = new Citizen(EntityId.New(), "Член", 25, new Position(0, 0));
        var nonMember = new Citizen(EntityId.New(), "Чужой", 30, new Position(0, 0));

        family.AddMember(member);

        Assert.True(family.HasMember(member));
        Assert.False(family.HasMember(nonMember));
    }

    [Fact]
    public void Family_MinimalRoomCount_CalculatesCorrectly()
    {
        var family = new Family(EntityId.New(), "Семья");

        // 0 членов → 1 комната
        Assert.Equal(1u, family.MinimalRoomCount);

        // 1 член → 1 комната
        family.AddMember(new Citizen(EntityId.New(), "1", 25, new Position(0, 0)));
        Assert.Equal(1u, family.MinimalRoomCount);

        // 2 члена → 1 комната
        family.AddMember(new Citizen(EntityId.New(), "2", 30, new Position(0, 0)));
        Assert.Equal(1u, family.MinimalRoomCount);

        // 3 члена → 2 комнаты
        family.AddMember(new Citizen(EntityId.New(), "3", 5, new Position(0, 0)));
        Assert.Equal(2u, family.MinimalRoomCount);

        // 4 члена → 2 комнаты
        family.AddMember(new Citizen(EntityId.New(), "4", 2, new Position(0, 0)));
        Assert.Equal(2u, family.MinimalRoomCount);

        // 5 членов → 3 комнаты
        family.AddMember(new Citizen(EntityId.New(), "5", 60, new Position(0, 0)));
        Assert.Equal(3u, family.MinimalRoomCount);
    }

    [Fact]
    public void Family_ToString_ContainsNameAndCount()
    {
        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(new Citizen(EntityId.New(), "Член1", 25, new Position(0, 0)));
        family.AddMember(new Citizen(EntityId.New(), "Член2", 30, new Position(0, 0)));

        var str = family.ToString();

        Assert.Contains("Петровы", str);
        Assert.Contains("2", str);
    }

    #endregion

    #region Household Tests

    [Fact]
    public void Household_Constructor_InitializesProperties()
    {
        var family = new Family(EntityId.New(), "Семья");
        var buildingId = EntityId.New();

        var household = new Household(family, buildingId);

        Assert.Equal(family, household.Family);
        Assert.Equal(buildingId, household.BuildingId);
        Assert.Equal(0.5, household.Satisfaction);
    }

    [Fact]
    public void Household_Constructor_NullFamilyThrows()
    {
        var buildingId = EntityId.New();

        Assert.Throws<ArgumentNullException>(() => new Household(null!, buildingId));
    }

    [Fact]
    public void Household_CanSetSatisfaction()
    {
        var family = new Family(EntityId.New(), "Семья");
        var household = new Household(family, EntityId.New());

        household.Satisfaction = 0.8;

        Assert.Equal(0.8, household.Satisfaction);
    }

    [Fact]
    public void Household_DaysInBuilding_CalculatesCorrectly()
    {
        var family = new Family(EntityId.New(), "Семья");
        var household = new Household(family, EntityId.New());

        // В день создания должно быть 0 дней
        Assert.Equal(0, household.DaysInBuilding);
    }

    [Fact]
    public void Household_CanChangeBuildingThroughPopulationService()
    {
        var service = new PopulationService();
        var family = new Family(EntityId.New(), "Семья");
        var oldBuildingId = EntityId.New();
        var newBuildingId = EntityId.New();

        var household = new Household(family, oldBuildingId);
        Assert.True(service.RegisterHousehold(household));
        Assert.Equal(oldBuildingId, household.BuildingId);

        Assert.True(service.MoveHousehold(family.Id, newBuildingId));

        Assert.Equal(newBuildingId, household.BuildingId);
    }

    #endregion

    #region PopulationService Tests

    [Fact]
    public void PopulationService_Find_ReturnsCitizen()
    {
        var service = new PopulationService();
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));

        service.RegisterCitizen(citizen);

        var found = service.Find(citizen.Id);

        Assert.NotNull(found);
        Assert.Equal(citizen.Id, found.Id);
    }

    [Fact]
    public void PopulationService_Find_ReturnsNull()
    {
        var service = new PopulationService();

        var found = service.Find(EntityId.New());

        Assert.Null(found);
    }

    [Fact]
    public void PopulationService_RegisterCitizen_AddsToPeople()
    {
        var service = new PopulationService();
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));

        var result = service.RegisterCitizen(citizen);

        Assert.True(result);
        Assert.Single(service.People);
    }

    [Fact]
    public void PopulationService_RegisterCitizen_DuplicateFails()
    {
        var service = new PopulationService();
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));

        service.RegisterCitizen(citizen);
        var result = service.RegisterCitizen(citizen);

        Assert.False(result);
        Assert.Single(service.People);
    }

    [Fact]
    public void PopulationService_RegisterCitizen_NullThrows()
    {
        var service = new PopulationService();

        Assert.Throws<ArgumentNullException>(() => service.RegisterCitizen(null!));
    }

    [Fact]
    public void PopulationService_UnregisterCitizen_RemovesPerson()
    {
        var service = new PopulationService();
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));

        service.RegisterCitizen(citizen);
        Assert.Single(service.People);

        var result = service.UnregisterCitizen(citizen.Id);

        Assert.True(result);
        Assert.Empty(service.People);
    }

    [Fact]
    public void PopulationService_UnregisterCitizen_NonExistentFails()
    {
        var service = new PopulationService();

        var result = service.UnregisterCitizen(EntityId.New());

        Assert.False(result);
    }

    [Fact]
    public void PopulationService_RegisterFamily_AddToFamilies()
    {
        var service = new PopulationService();
        var family = new Family(EntityId.New(), "Семья");

        var result = service.RegisterFamily(family);

        Assert.True(result);
        Assert.Single(service.Families);
    }

    [Fact]
    public void PopulationService_RegisterHousehold_AddsAndLinksResidents()
    {
        var service = new PopulationService();
        var family = new Family(EntityId.New(), "Семья");
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var buildingId = EntityId.New();

        family.AddMember(citizen);
        service.RegisterCitizen(citizen);

        var household = new Household(family, buildingId);
        var result = service.RegisterHousehold(household);

        Assert.True(result);
        Assert.Single(service.Households);

        // Проверяем, что жилье связано
        var residence = service.GetResidence(citizen.Id);
        Assert.Equal(buildingId, residence);
    }

    [Fact]
    public void PopulationService_MoveHousehold_UpdatesResidence()
    {
        var service = new PopulationService();
        var family = new Family(EntityId.New(), "Семья");
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var oldBuildingId = EntityId.New();
        var newBuildingId = EntityId.New();

        family.AddMember(citizen);
        service.RegisterCitizen(citizen);
        var household = new Household(family, oldBuildingId);
        service.RegisterHousehold(household);

        // Переселяем
        var result = service.MoveHousehold(family.Id, newBuildingId);

        Assert.True(result);
        Assert.Equal(newBuildingId, household.BuildingId);
        Assert.Equal(newBuildingId, service.GetResidence(citizen.Id));
    }

    [Fact]
    public void PopulationService_GetResidentsOfBuilding_ReturnsCorrectCitizens()
    {
        var service = new PopulationService();
        var family = new Family(EntityId.New(), "Семья");
        var citizen1 = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var citizen2 = new Citizen(EntityId.New(), "Мария", 28, new Position(0, 0));
        var buildingId = EntityId.New();

        family.AddMember(citizen1);
        family.AddMember(citizen2);
        service.RegisterCitizen(citizen1);
        service.RegisterCitizen(citizen2);

        var household = new Household(family, buildingId);
        service.RegisterHousehold(household);

        var residents = service.GetResidentsOfBuilding(buildingId);

        Assert.Equal(2, residents.Count);
        Assert.Contains(citizen1, residents);
        Assert.Contains(citizen2, residents);
    }

    [Fact]
    public void PopulationService_GetResidentsOfBuilding_EmptyForUnknownBuilding()
    {
        var service = new PopulationService();

        var residents = service.GetResidentsOfBuilding(EntityId.New());

        Assert.Empty(residents);
    }

    [Fact]
    public void PopulationService_UnregisterHousehold_RemovesResidence()
    {
        var service = new PopulationService();
        var family = new Family(EntityId.New(), "Семья");
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));

        family.AddMember(citizen);
        service.RegisterCitizen(citizen);
        service.RegisterFamily(family);  // Регистрируем семью
        var household = new Household(family, EntityId.New());
        service.RegisterHousehold(household);

        Assert.NotNull(service.GetResidence(citizen.Id));

        var result = service.UnregisterHousehold(family.Id);

        Assert.True(result);
        Assert.Null(service.GetResidence(citizen.Id));
    }

    [Fact]
    public void PopulationService_Clear_RemovesAllData()
    {
        var service = new PopulationService();
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));

        service.RegisterCitizen(citizen);
        Assert.Single(service.People);

        service.Clear();

        Assert.Empty(service.People);
    }

    [Fact]
    public void PopulationService_UnregisterCitizen_RemovesFromResidenceMap()
    {
        var service = new PopulationService();
        var family = new Family(EntityId.New(), "Семья");
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var buildingId = EntityId.New();

        family.AddMember(citizen);
        service.RegisterCitizen(citizen);
        var household = new Household(family, buildingId);
        service.RegisterHousehold(household);

        Assert.NotNull(service.GetResidence(citizen.Id));

        service.UnregisterCitizen(citizen.Id);

        Assert.Null(service.GetResidence(citizen.Id));
    }

    [Fact]
    public void PopulationService_SyncsCitizenHomeId_OnRegisterMoveAndUnregisterHousehold()
    {
        var service = new PopulationService();
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        var family = new Family(EntityId.New(), "Петровы");
        family.AddMember(citizen);

        service.RegisterCitizen(citizen);
        Assert.Null(citizen.HomeId);

        var building1 = EntityId.New();
        var building2 = EntityId.New();
        var household = new Household(family, building1);

        Assert.True(service.RegisterHousehold(household));
        Assert.Equal(building1, citizen.HomeId);
        Assert.Equal(building1, service.GetResidence(citizen.Id));
        Assert.Equal(building1, household.BuildingId);

        Assert.True(service.MoveHousehold(family.Id, building2));
        Assert.Equal(building2, citizen.HomeId);
        Assert.Equal(building2, service.GetResidence(citizen.Id));
        Assert.Equal(building2, household.BuildingId);

        Assert.True(service.UnregisterHousehold(family.Id));
        Assert.Null(citizen.HomeId);
        Assert.Null(service.GetResidence(citizen.Id));
    }

    [Fact]
    public void PopulationService_RegisterHousehold_WithUnregisteredCitizen_FailsAtomically()
    {
        var service = new PopulationService();
        var registered = new Citizen(EntityId.New(), "Зарегистрированный", 30, new Position(0, 0));
        var unregistered = new Citizen(EntityId.New(), "Незарегистрированный", 25, new Position(0, 0));

        service.RegisterCitizen(registered);

        var family = new Family(EntityId.New(), "Семья");
        family.AddMember(registered);
        family.AddMember(unregistered);

        var buildingId = EntityId.New();
        var household = new Household(family, buildingId);

        Assert.False(service.RegisterHousehold(household));
        Assert.Empty(service.Households);
        Assert.Empty(service.Families);
        Assert.Null(registered.HomeId);
        Assert.Null(service.GetResidence(registered.Id));
        Assert.Empty(service.GetResidentsOfBuilding(buildingId));
    }

    [Fact]
    public void PopulationService_DoesNotAllowCitizenInTwoHouseholdsOrFamilies()
    {
        var service = new PopulationService();
        var citizen = new Citizen(EntityId.New(), "Иван", 30, new Position(0, 0));
        service.RegisterCitizen(citizen);

        var family1 = new Family(EntityId.New(), "Первая");
        family1.AddMember(citizen);
        var family2 = new Family(EntityId.New(), "Вторая");
        family2.AddMember(citizen);

        var building1 = EntityId.New();
        var building2 = EntityId.New();

        Assert.True(service.RegisterHousehold(new Household(family1, building1)));
        Assert.False(service.RegisterFamily(family2));
        Assert.False(service.RegisterHousehold(new Household(family2, building2)));

        // Состояние первого домохозяйства не пострадало
        Assert.Equal(building1, citizen.HomeId);
        Assert.Equal(building1, service.GetResidence(citizen.Id));
        Assert.Single(service.Households);
    }

    [Fact]
    public void PopulationService_FamilyCompositionChanges_SyncWithHousehold()
    {
        var service = new PopulationService();
        var parent = new Citizen(EntityId.New(), "Родитель", 30, new Position(0, 0));
        var child = new Citizen(EntityId.New(), "Ребёнок", 1, new Position(0, 0));
        var outsider = new Citizen(EntityId.New(), "Чужой", 40, new Position(0, 0));

        service.RegisterCitizen(parent);
        service.RegisterCitizen(child);
        // outsider намеренно не регистрируем в PopulationService

        var family = new Family(EntityId.New(), "Семья");
        family.AddMember(parent);

        var buildingId = EntityId.New();
        Assert.True(service.RegisterHousehold(new Household(family, buildingId)));

        // Добавляем нового члена через Family.AddMember — должен автоматически прописаться в доме
        family.AddMember(child);
        Assert.Equal(buildingId, child.HomeId);
        Assert.Equal(buildingId, service.GetResidence(child.Id));
        Assert.Equal(2, service.GetResidentsOfBuilding(buildingId).Count);

        // Попытка добавить незарегистрированного гражданина в зарегистрированную семью должна отклоняться
        Assert.False(service.AddMemberToFamily(family.Id, outsider));
        Assert.Throws<InvalidOperationException>(() => family.AddMember(outsider));
        Assert.Equal(2, family.MemberCount);

        // Удаление члена семьи сбрасывает его жильё
        family.RemoveMember(child);
        Assert.Null(child.HomeId);
        Assert.Null(service.GetResidence(child.Id));
        Assert.Single(service.GetResidentsOfBuilding(buildingId));
    }

    [Fact]
    public void Household_WithEmptyBuildingId_ThrowsArgumentException()
    {
        var family = new Family(EntityId.New(), "Семья");

        Assert.Throws<ArgumentException>(() => new Household(family, default));
    }

    #endregion
}
