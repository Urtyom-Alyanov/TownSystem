using System.Text.Json.Nodes;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.Population;

/// <summary>
/// Модуль управления населением города.
/// Управляет горожанами, семьями и их размещением по жилым зданиям.
/// Работает совместно с ResidentialModule через IBuildingRegistry.
/// </summary>
public sealed class PopulationModule : ISimulationModule, ISaveable
{
    private PopulationService? populationService;
    private IBuildingRegistry? buildingRegistry;
    private IWorld? world;

    public string Name => "Population";
    public string SaveKey => "population";
    public int Order => -48; // После Residential (-50)

    /// <summary>
    /// Инициализирует модуль: создает сервис населения и регистрирует его.
    /// </summary>
    public void Initialize(IWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        this.world = world;

        // Создаем сервис населения
        populationService = new PopulationService();

        // Регистрируем сервис для других модулей
        world.RegisterService<IPopulationService>(populationService);
    }

    /// <summary>
    /// Вызывается после инициализации всех модулей.
    /// Получаем доступ к реестру зданий из Residential модуля.
    /// </summary>
    public void OnWorldReady()
    {
        if (world == null)
            throw new InvalidOperationException("Initialize должен быть вызван перед OnWorldReady");

        buildingRegistry = world.GetService<IBuildingRegistry>();
    }

    /// <summary>
    /// Выполняет один такт симуляции.
    /// Обновляет состояние населения (не требуется в базовой версии).
    /// </summary>
    public void Tick(TickContext context)
    {
        // В базовой версии Etap 4 нет специальной логики на такт
        // Это может быть расширено в будущих этапах
    }

    /// <summary>
    /// Сохраняет состояние модуля в JSON.
    /// </summary>
    public JsonNode Save()
    {
        if (populationService == null)
            return new JsonObject();

        var root = new JsonObject();
        var citizensArray = new JsonArray();
        var familiesArray = new JsonArray();
        var householdsArray = new JsonArray();

        // Сохраняем горожан
        foreach (var person in populationService.People.OfType<Citizen>())
        {
            var citizenObj = new JsonObject
            {
                ["id"] = person.Id.Value.ToString("N"),
                ["name"] = person.Name,
                ["age"] = person.Age,
                ["positionX"] = person.Position.X,
                ["positionY"] = person.Position.Y,
                ["workplaceId"] = person.WorkplaceId?.Value.ToString("N") ?? "",
                ["education"] = person.Education.ToString(),
                ["health"] = person.Health
            };
            citizensArray.Add(citizenObj);
        }

        // Сохраняем семьи
        foreach (var family in populationService.Families)
        {
            var familyObj = new JsonObject
            {
                ["id"] = family.Id.Value.ToString("N"),
                ["name"] = family.Name,
                ["memberIds"] = new JsonArray(family.Members.Select(m => JsonValue.Create(m.Id.Value.ToString("N"))).ToArray())
            };
            familiesArray.Add(familyObj);
        }

        // Сохраняем домохозяйства
        foreach (var household in populationService.Households)
        {
            var householdObj = new JsonObject
            {
                ["familyId"] = household.Family.Id.Value.ToString("N"),
                ["buildingId"] = household.BuildingId.Value.ToString("N"),
                ["moveInDate"] = household.MoveInDate.Ticks,
                ["satisfaction"] = household.Satisfaction
            };
            householdsArray.Add(householdObj);
        }

        root["citizens"] = citizensArray;
        root["families"] = familiesArray;
        root["households"] = householdsArray;

        return root;
    }

    /// <summary>
    /// Восстанавливает состояние модуля из JSON.
    /// </summary>
    public void Load(JsonNode state)
    {
        if (populationService == null)
            return;

        // Очищаем текущее состояние
        populationService.Clear();

        // Восстанавливаем горожан
        if (state?["citizens"] is JsonArray citizensArray)
        {
            var citizens = new Dictionary<EntityId, Citizen>();

            foreach (var citizenNode in citizensArray.OfType<JsonObject>())
            {
                try
                {
                    var id = new EntityId(Guid.ParseExact(citizenNode["id"]!.GetValue<string>(), "N"));
                    var name = citizenNode["name"]!.GetValue<string>();
                    var age = citizenNode["age"]!.GetValue<uint>();
                    var positionX = citizenNode["positionX"]!.GetValue<double>();
                    var positionY = citizenNode["positionY"]!.GetValue<double>();
                    var workplaceIdStr = citizenNode["workplaceId"]!.GetValue<string>();
                    var education = Enum.Parse<EducationLevel>(citizenNode["education"]!.GetValue<string>());
                    var health = citizenNode["health"]!.GetValue<double>();

                    var citizen = new Citizen(
                        id,
                        name,
                        age,
                        new Position(positionX, positionY)
                    )
                    {
                        WorkplaceId = string.IsNullOrEmpty(workplaceIdStr) ? null : new EntityId(Guid.ParseExact(workplaceIdStr, "N")),
                        Education = education,
                        Health = health
                    };

                    populationService.RegisterCitizen(citizen);
                    citizens[id] = citizen;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке горожанина: {ex.Message}");
                }
            }
        }

        // Восстанавливаем семьи
        if (state?["families"] is JsonArray familiesArray)
        {
            var families = new Dictionary<EntityId, Family>();

            foreach (var familyNode in familiesArray.OfType<JsonObject>())
            {
                try
                {
                    var id = new EntityId(Guid.ParseExact(familyNode["id"]!.GetValue<string>(), "N"));
                    var name = familyNode["name"]!.GetValue<string>();

                    var family = new Family(id, name);

                    // Добавляем членов семьи
                    if (familyNode["memberIds"] is JsonArray memberIds)
                    {
                        foreach (var memberId in memberIds.OfType<JsonValue>())
                        {
                            var citizenId = new EntityId(Guid.ParseExact(memberId.GetValue<string>(), "N"));
                            if (populationService.FindCitizen(citizenId) is Citizen citizen)
                            {
                                family.AddMember(citizen);
                            }
                        }
                    }

                    populationService.RegisterFamily(family);
                    families[id] = family;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке семьи: {ex.Message}");
                }
            }
        }

        // Восстанавливаем домохозяйства
        if (state?["households"] is JsonArray householdsArray)
        {
            foreach (var householdNode in householdsArray.OfType<JsonObject>())
            {
                try
                {
                    var familyId = new EntityId(Guid.ParseExact(householdNode["familyId"]!.GetValue<string>(), "N"));
                    var buildingId = new EntityId(Guid.ParseExact(householdNode["buildingId"]!.GetValue<string>(), "N"));
                    var satisfaction = householdNode["satisfaction"]!.GetValue<double>();

                    if (populationService.FindFamily(familyId) is Family family)
                    {
                        var household = new Household(family, buildingId)
                        {
                            Satisfaction = satisfaction
                        };

                        populationService.RegisterHousehold(household);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке домохозяйства: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Получает сервис населения для прямого доступа (внутреннее использование).
    /// </summary>
    public PopulationService? GetService() => populationService;
}
