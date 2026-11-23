# Руководство по интеграции модулей Residential и Population

## Статус реализации

**Этап 1-5 завершены:**
- ✅ ResidentialBuilding модели и характеристики (23 теста)
- ✅ ResidentialBuildingRegistry и ResidentialModule (27 тестов)
- ✅ Citizen, Family, Household модели (32 теста)
- ✅ PopulationModule с Save/Load (12 тестов)
- ✅ Интеграционные тесты взаимодействия (5 тестов)
- ✅ **Всего: 150 тестов, все пройдены**

## Файлы модулей

### Residential Module
- `src/TownSystem.Residential/ResidentialBuilding.cs` - модель жилого здания
- `src/TownSystem.Residential/ResidentialBuildingRegistry.cs` - реестр зданий (IBuildingRegistry)
- `src/TownSystem.Residential/ResidentialModule.cs` - основной модуль (ISimulationModule, ISaveable)

### Population Module
- `src/TownSystem.Population/Citizen.cs` - модель горожанина (IPerson)
- `src/TownSystem.Population/Family.cs` - модель семьи
- `src/TownSystem.Population/Household.cs` - модель домохозяйства
- `src/TownSystem.Population/PopulationService.cs` - сервис управления населением (IPopulationService)
- `src/TownSystem.Population/PopulationModule.cs` - основной модуль (ISimulationModule, ISaveable)

## Регистрация модулей в UIApp

Для использования модулей в UI необходимо:

### 1. Добавить ProjectReferences в UIApp.csproj
```xml
<ItemGroup>
    <ProjectReference Include="..\..\src\TownSystem.Residential\TownSystem.Residential.csproj" />
    <ProjectReference Include="..\..\src\TownSystem.Population\TownSystem.Population.csproj" />
</ItemGroup>
```

### 2. Зарегистрировать модули в GameBootstrapper (или аналогичном классе инициализации)
```csharp
var residentialModule = new ResidentialModule();
var populationModule = new PopulationModule();

modules.Add(residentialModule);  // Order = -50
modules.Add(populationModule);   // Order = -48
```

**Порядок инициализации важен:** ResidentialModule должен инициализироваться раньше PopulationModule, т.к. Population зависит от Building Registry.

## Используемые интерфейсы

### IBuildingRegistry (SharedLib.Contracts)
Реализует: `ResidentialBuildingRegistry`
Методы:
- `bool TryAdd(IBuilding building)`
- `IBuilding? Find(EntityId id)`
- `IReadOnlyCollection<IBuilding> All`

### IPopulationService (SharedLib.Contracts)
Реализует: `PopulationService`
Методы (базовый интерфейс):
- `IReadOnlyCollection<IPerson> People`
- `IPerson? Find(EntityId id)`

Дополнительные методы в PopulationService:
- `bool RegisterCitizen(Citizen citizen)`
- `bool RegisterFamily(Family family)`
- `bool RegisterHousehold(Household household)`
- `EntityId? GetResidence(EntityId citizenId)`
- `IReadOnlyCollection<Citizen> GetResidentsOfBuilding(EntityId buildingId)`

## Сохранение и загрузка

Оба модуля реализуют `ISaveable`:
```csharp
JsonNode Save()       // Сохраняет состояние в JSON
void Load(JsonNode?)  // Загружает состояние из JSON
```

**Порядок важен при загрузке:**
1. Загрузить ResidentialModule (восстановит здания)
2. Загрузить PopulationModule (восстановит граждан и их размещение)

## Характеристики зданий

| Тип | Вместимость | Формула |
|-----|------------|---------|
| PrivateHouse | 4 | base × floors |
| LowRise | 8 | base × floors |
| MidRise | 12 | base × floors |
| HighRise | 15 | base × floors |
| Dormitory | 20 | base × floors |

Износ: 0.00001 в день (100 лет до полного износа)

## Тестирование

Все интеграционные тесты находятся в:
`tests/TownSystem.Residential.Tests/IntegrationTests.cs`

Запуск:
```bash
dotnet test -c Debug
```

Результат: 150 тестов пройдено ✅

## Следующие шаги для команды

1. **UI Integration** - добавить визуализацию зданий и населения
2. **Economy System** - интеграция со системой экономики
3. **Services** - врачи, школы, работа для граждан
4. **Events** - система событий (рождение, смерть, переезд)
5. **Statistics** - сбор статистики по зданиям и населению

## Контакт

Модули готовы к интеграции. Все тесты пройдены. Нет блокирующих ошибок.
