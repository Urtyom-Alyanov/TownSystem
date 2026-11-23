using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.Population;

/// <summary>
/// Сервис населения города.
/// Является единственным авторитетным механизмом управления составом зарегистрированных семей
/// и местом жительства горожан (синхронизирует Citizen.HomeId, Household.BuildingId и residenceMap).
/// </summary>
public sealed class PopulationService : IPopulationService
{
    private readonly Dictionary<EntityId, Citizen> peopleDictionary = new();
    private readonly Dictionary<EntityId, Family> familiesDictionary = new();
    private readonly Dictionary<EntityId, Household> householdsDictionary = new();

    /// <summary>
    /// Внутренний индекс: PersonId → BuildingId для быстрого поиска жилья.
    /// Всегда поддерживается синхронно с Citizen.HomeId и Household.BuildingId.
    /// </summary>
    private readonly Dictionary<EntityId, EntityId> residenceMap = new();

    /// <summary>
    /// Все живые люди в городе.
    /// </summary>
    public IReadOnlyCollection<IPerson> People => peopleDictionary.Values;

    /// <summary>
    /// Все семьи в городе.
    /// </summary>
    public IReadOnlyCollection<Family> Families => familiesDictionary.Values;

    /// <summary>
    /// Все домохозяйства в городе.
    /// </summary>
    public IReadOnlyCollection<Household> Households => householdsDictionary.Values;

    /// <summary>
    /// Находит горожанина по ID.
    /// </summary>
    /// <param name="id">ID горожанина</param>
    /// <returns>Горожанин или null</returns>
    public IPerson? Find(EntityId id)
    {
        peopleDictionary.TryGetValue(id, out var person);
        return person;
    }

    /// <summary>
    /// Находит горожанина с типом Citizen.
    /// </summary>
    /// <param name="id">ID горожанина</param>
    /// <returns>Горожанин или null</returns>
    public Citizen? FindCitizen(EntityId id)
    {
        peopleDictionary.TryGetValue(id, out var person);
        return person;
    }

    /// <summary>
    /// Находит семью по ID.
    /// </summary>
    /// <param name="id">ID семьи</param>
    /// <returns>Семья или null</returns>
    public Family? FindFamily(EntityId id)
    {
        familiesDictionary.TryGetValue(id, out var family);
        return family;
    }

    /// <summary>
    /// Находит домохозяйство по ID семьи.
    /// </summary>
    /// <param name="id">ID семьи домохозяйства</param>
    /// <returns>Домохозяйство или null</returns>
    public Household? FindHousehold(EntityId id)
    {
        householdsDictionary.TryGetValue(id, out var household);
        return household;
    }

    /// <summary>
    /// Регистрирует нового горожанина.
    /// </summary>
    /// <param name="citizen">Горожанин</param>
    /// <returns>true если успешно добавлен, false если уже существует</returns>
    public bool RegisterCitizen(Citizen citizen)
    {
        if (citizen == null)
            throw new ArgumentNullException(nameof(citizen));

        if (peopleDictionary.ContainsKey(citizen.Id))
            return false;

        citizen.HomeId = null;
        peopleDictionary[citizen.Id] = citizen;
        return true;
    }

    /// <summary>
    /// Удаляет горожанина из города (смерть, эмиграция).
    /// Удаляет его из peopleDictionary, residenceMap и состава семьи;
    /// если семья была заселена и осталась без участников, удаляет пустое домохозяйство.
    /// </summary>
    /// <param name="citizenId">ID горожанина</param>
    /// <returns>true если успешно удалён</returns>
    public bool UnregisterCitizen(EntityId citizenId)
    {
        if (!peopleDictionary.Remove(citizenId, out var citizen))
            return false;

        residenceMap.Remove(citizenId);
        citizen.HomeId = null;

        foreach (var family in familiesDictionary.Values)
        {
            if (family.RemoveMemberInternal(citizen))
            {
                if (family.MemberCount == 0)
                    UnregisterHousehold(family.Id);

                break;
            }
        }

        return true;
    }

    /// <summary>
    /// Регистрирует новую семью.
    /// Проверяет, что все члены семьи зарегистрированы в сервисе и не состоят в других семьях или жилье.
    /// </summary>
    /// <param name="family">Семья</param>
    /// <returns>true если успешно добавлена, false если семья уже существует или состав некорректен</returns>
    public bool RegisterFamily(Family family)
    {
        if (family == null)
            throw new ArgumentNullException(nameof(family));

        if (familiesDictionary.ContainsKey(family.Id))
            return false;

        if (family.OwnerService != null && !ReferenceEquals(family.OwnerService, this))
            return false;

        if (!CanRegisterFamilyMembers(family))
            return false;

        familiesDictionary[family.Id] = family;
        family.AttachToService(this);
        return true;
    }

    /// <summary>
    /// Удаляет семью из сервиса без удаления самих граждан из города.
    /// Связанное домохозяйство (если оно было) расформировывается, у жильцов сбрасывается место проживания.
    /// </summary>
    /// <param name="familyId">ID семьи</param>
    /// <returns>true если семья найдена и удалена; false если семья не найдена</returns>
    public bool UnregisterFamily(EntityId familyId)
    {
        if (!familiesDictionary.TryGetValue(familyId, out var family))
            return false;

        UnregisterHousehold(familyId);

        familiesDictionary.Remove(familyId);
        family.ClearMembersInternal();
        family.DetachFromService();
        return true;
    }

    /// <summary>
    /// Добавляет зарегистрированного гражданина в зарегистрированную семью.
    /// Если у семьи есть домохозяйство, синхронно назначает место жительства.
    /// </summary>
    public bool AddMemberToFamily(EntityId familyId, Citizen citizen)
    {
        if (citizen == null)
            throw new ArgumentNullException(nameof(citizen));

        if (!familiesDictionary.TryGetValue(familyId, out var family))
            return false;

        if (!IsRegisteredCitizenInstance(citizen))
            return false;

        if (family.HasMember(citizen))
            return false;

        if (IsCitizenInAnyRegisteredFamily(citizen.Id, excludedFamilyId: familyId))
            return false;

        if (householdsDictionary.TryGetValue(familyId, out var household))
        {
            if (residenceMap.ContainsKey(citizen.Id) || citizen.HomeId != null)
                return false;

            family.AddMemberInternal(citizen);
            residenceMap[citizen.Id] = household.BuildingId;
            citizen.HomeId = household.BuildingId;
            return true;
        }

        family.AddMemberInternal(citizen);
        return true;
    }

    /// <summary>
    /// Удаляет гражданина из зарегистрированной семьи и сбрасывает его проживание, если семья заселена.
    /// Если у заселённой семьи не остаётся членов, пустое домохозяйство расформировывается.
    /// </summary>
    public bool RemoveMemberFromFamily(EntityId familyId, EntityId citizenId)
    {
        if (!familiesDictionary.TryGetValue(familyId, out var family))
            return false;

        var member = family.Members.FirstOrDefault(m => m.Id == citizenId);
        if (member == null)
            return false;

        family.RemoveMemberInternal(member);

        if (householdsDictionary.ContainsKey(familyId))
        {
            residenceMap.Remove(citizenId);
            member.HomeId = null;

            if (family.MemberCount == 0)
                UnregisterHousehold(familyId);
        }

        return true;
    }

    /// <summary>
    /// Регистрирует домохозяйство (семью в доме).
    /// Операция атомарна: при нарушении любого условия состояние не изменяется.
    /// </summary>
    /// <param name="household">Домохозяйство</param>
    /// <returns>true если успешно добавлено, false при конфликте или некорректных данных</returns>
    public bool RegisterHousehold(Household household)
    {
        if (household == null)
            throw new ArgumentNullException(nameof(household));

        var family = household.Family;

        if (household.BuildingId == default)
            return false;

        if (householdsDictionary.ContainsKey(family.Id))
            return false;

        if (family.OwnerService != null && !ReferenceEquals(family.OwnerService, this))
            return false;

        bool familyAlreadyRegistered = familiesDictionary.TryGetValue(family.Id, out var existingFamily);
        if (familyAlreadyRegistered && !ReferenceEquals(existingFamily, family))
            return false;

        // Проверяем всех участников до внесения любых изменений
        foreach (var member in family.Members)
        {
            if (!IsRegisteredCitizenInstance(member))
                return false;

            if (residenceMap.ContainsKey(member.Id) || member.HomeId != null)
                return false;

            if (IsCitizenInAnyRegisteredFamily(member.Id, excludedFamilyId: family.Id))
                return false;
        }

        if (!familyAlreadyRegistered)
        {
            familiesDictionary[family.Id] = family;
            family.AttachToService(this);
        }

        householdsDictionary[family.Id] = household;

        foreach (var member in family.Members)
        {
            residenceMap[member.Id] = household.BuildingId;
            member.HomeId = household.BuildingId;
        }

        return true;
    }

    /// <summary>
    /// Переселяет домохозяйство в новое здание.
    /// Атомарно обновляет Household.BuildingId, Citizen.HomeId и residenceMap.
    /// </summary>
    /// <param name="householdFamilyId">ID семьи домохозяйства</param>
    /// <param name="newBuildingId">ID нового здания</param>
    /// <returns>true если успешно переселено, false если домохозяйство не найдено или ID здания пуст</returns>
    public bool MoveHousehold(EntityId householdFamilyId, EntityId newBuildingId)
    {
        if (newBuildingId == default)
            return false;

        if (!householdsDictionary.TryGetValue(householdFamilyId, out var household))
            return false;

        foreach (var member in household.Family.Members)
        {
            if (!IsRegisteredCitizenInstance(member))
                return false;
        }

        household.BuildingId = newBuildingId;

        foreach (var member in household.Family.Members)
        {
            residenceMap[member.Id] = newBuildingId;
            member.HomeId = newBuildingId;
        }

        return true;
    }

    /// <summary>
    /// Возвращает ID здания, где живёт горожанин.
    /// </summary>
    /// <param name="citizenId">ID горожанина</param>
    /// <returns>ID здания или null если нет жилья</returns>
    public EntityId? GetResidence(EntityId citizenId)
    {
        if (residenceMap.TryGetValue(citizenId, out var buildingId) && buildingId != default)
            return buildingId;

        return null;
    }

    /// <summary>
    /// Получает всех горожан в определённом здании.
    /// </summary>
    /// <param name="buildingId">ID здания</param>
    /// <returns>Коллекция горожан в этом здании</returns>
    public IReadOnlyCollection<Citizen> GetResidentsOfBuilding(EntityId buildingId)
    {
        var result = new List<Citizen>();
        foreach (var kvp in residenceMap)
        {
            if (kvp.Value == buildingId && peopleDictionary.TryGetValue(kvp.Key, out var citizen))
                result.Add(citizen);
        }

        return result;
    }

    /// <summary>
    /// Удаляет домохозяйство (выселение семьи без удаления самих граждан и семьи).
    /// </summary>
    /// <param name="householdFamilyId">ID семьи домохозяйства</param>
    /// <returns>true если успешно удалено</returns>
    public bool UnregisterHousehold(EntityId householdFamilyId)
    {
        if (!householdsDictionary.TryGetValue(householdFamilyId, out var household))
            return false;

        foreach (var member in household.Family.Members)
        {
            residenceMap.Remove(member.Id);
            member.HomeId = null;
        }

        householdsDictionary.Remove(householdFamilyId);
        return true;
    }

    /// <summary>
    /// Очищает все данные (для перезагрузки).
    /// </summary>
    public void Clear()
    {
        foreach (var citizen in peopleDictionary.Values)
            citizen.HomeId = null;

        foreach (var family in familiesDictionary.Values)
            family.DetachFromService();

        peopleDictionary.Clear();
        familiesDictionary.Clear();
        householdsDictionary.Clear();
        residenceMap.Clear();
    }

    private bool IsRegisteredCitizenInstance(Citizen citizen)
    {
        return peopleDictionary.TryGetValue(citizen.Id, out var registered) && ReferenceEquals(registered, citizen);
    }

    private bool IsCitizenInAnyRegisteredFamily(EntityId citizenId, EntityId? excludedFamilyId = null)
    {
        foreach (var family in familiesDictionary.Values)
        {
            if (excludedFamilyId.HasValue && family.Id == excludedFamilyId.Value)
                continue;

            if (family.Members.Any(m => m.Id == citizenId))
                return true;
        }

        return false;
    }

    private bool CanRegisterFamilyMembers(Family family)
    {
        foreach (var member in family.Members)
        {
            if (!IsRegisteredCitizenInstance(member))
                return false;

            if (IsCitizenInAnyRegisteredFamily(member.Id, excludedFamilyId: family.Id))
                return false;

            if (residenceMap.ContainsKey(member.Id) || member.HomeId != null)
                return false;
        }

        return true;
    }
}