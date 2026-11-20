# Как подключить свой модуль

## 1. Логика (src/TownSystem.<Sphere>)

```csharp
public sealed class PoliceModule : ISimulationModule, ISaveable
{
    private IWorld _world = null!;
    private readonly List<PatrolCar> _cars = new();

    public string Name => "Police";
    public string SaveKey => "police";

    public void Initialize(IWorld world)
    {
        _world = world;
        world.RegisterService<IPoliceService>(new PoliceService(_cars)); // если нужен другим модулям
        world.Events.Subscribe<EmergencyReported>(OnEmergency);          // реакция на бедствия
    }

    public void OnWorldReady()
    {
        var roads = _world.GetService<IRoadNetwork>(); // чужие сервисы — только здесь или в Tick
    }

    public void Tick(TickContext ctx) { foreach (var car in _cars) car.Update(ctx.Delta); }

    public JsonNode Save() => /* состояние в JsonObject */;
    public void Load(JsonNode state) { /* восстановить */ }
}
```

## 2. Интерфейс (ui/TownSystem.<Sphere>.UI)

```csharp
public sealed class PoliceViewModel : ModuleViewModel
{
    public EntityListViewModel<PatrolCar> Cars { get; } = new();
    public PoliceViewModel(IWorld world) : base(world, "Полиция") { Refresh(); }
    public override void Refresh() => Cars.Load(/* данные модуля */);
}

public sealed class PoliceUi : IModuleUi
{
    public string Title => "Полиция";
    public ModuleViewModel CreateViewModel(IWorld world) => new PoliceViewModel(world);
    public ResourceDictionary? Templates { get; } = new ResourceDictionary { Source = new Uri("pack://application:,,,/TownSystem.Police.UI;component/Templates.xaml") };
}
```

`Templates.xaml`:
```xml
<DataTemplate DataType="{x:Type vm:PoliceViewModel}"><views:PoliceView/></DataTemplate>
```

## 3. Регистрация (одна строка в оболочке, делает владелец варианта 6)

```csharp
catalog.Register(new PoliceModule(), new PoliceUi());
```

## 4. Оболочка (вариант 6) — вкладки

```xml
<TabControl ItemsSource="{Binding Tabs}">
    <TabControl.ItemTemplate><DataTemplate><TextBlock Text="{Binding Title}"/></DataTemplate></TabControl.ItemTemplate>
</TabControl>
```
Содержимое вкладки подбирается автоматически по DataTemplate из шага 2.

## Правила
- Между проектами сфер нет ссылок друг на друга: только SharedLib и шина событий.
- Чужие сервисы — только в `OnWorldReady` или `Tick`, не в `Initialize`.
- Порядок: `Order` меньше — раньше (карта -100, дороги -90, здания и люди -50, остальные 0).
- Изменения в SharedLib — только через PR с уведомлением всех.
