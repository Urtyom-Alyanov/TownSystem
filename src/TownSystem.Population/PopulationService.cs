using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.Population;

/// <summary>
/// Сервис население города.
/// Управляет всеми горожанами, семьями и их размещением по жилым зданиям.
/// </summary>
public class PopulationService : IPopulationService
{
    private readonly Dictionary<EntityId, Citizen> peopleDictionary = new();
    private readonly Dictionary<EntityId, Family> familiesDictionary = new();
    private readonly Dictionary<EntityId, Household> householdsDictionary = new();
    
    /// <summary>
    /// Внутренний словарь: PersonId → BuildingId для быстрого поиска жилья.
    /// Это источник истины для связи горожанина с зданием.
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
        return person as Citizen;
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
    /// Находит домохозяйство по ID.
    /// </summary>
    /// <param name="id">ID домохозяйства</param>
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

        peopleDictionary[citizen.Id] = citizen;
        return true;
    }

    /// <summary>
    /// Удаляет горожанина из города (смерть, эмиграция).
    /// </summary>
    /// <param name="citizenId">ID горожанина</param>
    /// <returns>true если успешно удалён</returns>
    public bool UnregisterCitizen(EntityId citizenId)
    {
        if (!peopleDictionary.Remove(citizenId))
            return false;

        // Удаляем из карты жилья
        residenceMap.Remove(citizenId);

        return true;
    }

    /// <summary>
    /// Регистрирует новую семью.
    /// </summary>
    /// <param name="family">Семья</param>
    /// <returns>true если успешно добавлена, false если уже существует</returns>
    public bool RegisterFamily(Family family)
    {
        if (family == null)
            throw new ArgumentNullException(nameof(family));

        if (familiesDictionary.ContainsKey(family.Id))
            return false;

        familiesDictionary[family.Id] = family;
        return true;
    }

    /// <summary>
    /// Регистрирует домохозяйство (семью в доме).
    /// </summary>
    /// <param name="household">Домохозяйство</param>
    /// <returns>true если успешно добавлено, false если уже существует</returns>
    public bool RegisterHousehold(Household household)
    {
        if (household == null)
            throw new ArgumentNullException(nameof(household));

        if (householdsDictionary.ContainsKey(household.Family.Id))
            return false;

        householdsDictionary[household.Family.Id] = household;

        // Устанавливаем жилье для всех членов семьи
        foreach (var member in household.Family.Members)
        {
            residenceMap[member.Id] = household.BuildingId;
        }

        return true;
    }

    /// <summary>
    /// Переселяет домохозяйство в новое здание.
    /// </summary>
    /// <param name="householdFamilyId">ID семьи домохозяйства</param>
    /// <param name="newBuildingId">ID нового здания</param>
    /// <returns>true если успешно переселено, false если домохозяйство не найдено</returns>
    public bool MoveHousehold(EntityId householdFamilyId, EntityId newBuildingId)
    {
        if (!householdsDictionary.TryGetValue(householdFamilyId, out var household))
            return false;

        var oldBuildingId = household.BuildingId;
        household.BuildingId = newBuildingId;

        // Обновляем карту жилья
        foreach (var member in household.Family.Members)
        {
            residenceMap[member.Id] = newBuildingId;
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
        residenceMap.TryGetValue(citizenId, out var buildingId);
        return buildingId == default ? null : buildingId;
    }

    /// <summary>
    /// Получает всех горожан в определённом здании.
    /// </summary>
    /// <param name="buildingId">ID здания</param>
    /// <returns>Коллекция горожан в этом здании</returns>
    public IReadOnlyCollection<Citizen> GetResidentsOfBuilding(EntityId buildingId)
    {
        return residenceMap
            .Where(kvp => kvp.Value == buildingId)
            .Select(kvp => peopleDictionary[kvp.Key])
            .ToList();
    }

    /// <summary>
    /// Удаляет домохозяйство (например, когда семья эмигрирует).
    /// </summary>
    /// <param name="householdFamilyId">ID семьи домохозяйства</param>
    /// <returns>true если успешно удалено</returns>
    public bool UnregisterHousehold(EntityId householdFamilyId)
    {
        if (!householdsDictionary.TryGetValue(householdFamilyId, out var household))
            return false;

        // Удаляем членов семьи из карты жилья
        foreach (var member in household.Family.Members)
        {
            residenceMap.Remove(member.Id);
        }

        householdsDictionary.Remove(householdFamilyId);

        return true;
    }

    /// <summary>
    /// Очищает все данные (для перезагрузки).
    /// </summary>
    public void Clear()
    {
        peopleDictionary.Clear();
        familiesDictionary.Clear();
        householdsDictionary.Clear();
        residenceMap.Clear();
    }
}
