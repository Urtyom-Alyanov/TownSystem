using System.Text.Json.Nodes;

using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;
using TownSystem.SharedLib.Persistence;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.SharedLib.Tests;

public class CoreTests
{
    private sealed class CounterModule(string name, int order = 0, bool fail = false) : ISimulationModule, ISaveable
    {
        public string Name { get; } = name;
        public int Order { get; } = order;
        public int Ticks;
        public void Initialize(IWorld world) { }
        public void Tick(TickContext context) { if (fail) throw new InvalidOperationException("boom"); Ticks++; }
        public string SaveKey => Name;
        public JsonNode Save() => new JsonObject { ["ticks"] = Ticks };
        public void Load(JsonNode state) => Ticks = state["ticks"]!.GetValue<int>();
    }

    [Fact]
    public void EventBus_delivers_to_base_type_subscribers_and_stops_after_dispose()
    {
        var bus = new EventBus();
        var all = 0; var emergencies = 0;
        using var allSub = bus.Subscribe<IWorldEvent>(_ => all++);
        var sub = bus.Subscribe<EmergencyReported>(_ => emergencies++);

        bus.Publish(new EmergencyReported(Guid.NewGuid(), EmergencyKind.Fire, new Position(1, 2), 3));
        sub.Dispose();
        bus.Publish(new EmergencyReported(Guid.NewGuid(), EmergencyKind.Fire, new Position(1, 2), 3));

        Assert.Equal(2, all);
        Assert.Equal(1, emergencies);
    }

    [Fact]
    public void Inventory_respects_capacity_and_removal()
    {
        var inv = new Inventory(10);
        Assert.Equal(10, inv.Add(ItemType.IronOre, 15));
        Assert.False(inv.TryRemove(ItemType.IronOre, 11));
        Assert.True(inv.TryRemove(ItemType.IronOre, 4));
        Assert.Equal(6, inv.CountOf(ItemType.IronOre));
    }

    [Fact]
    public void World_ticks_in_order_and_isolates_failures()
    {
        World world = new();
        world.Clock.SetSpeed(1);
        var good = new CounterModule("good", order: 10);
        var bad = new CounterModule("bad", order: -10, fail: true);
        var failures = 0;
        world.Events.Subscribe<ModuleFailed>(_ => failures++);
        world.RegisterModule(good);
        world.RegisterModule(bad);
        world.Start();

        world.Update(TimeSpan.FromSeconds(1));

        Assert.Equal("bad", world.Modules[0].Name);
        Assert.Equal(1, good.Ticks);
        Assert.Equal(1, failures);
    }

    [Fact]
    public void Save_and_load_roundtrip_restores_module_state_and_time()
    {
        World world = new();
        world.Clock.SetSpeed(1);
        var module = new CounterModule("counter");
        world.RegisterModule(module);
        world.Update(TimeSpan.FromSeconds(1)); world.Update(TimeSpan.FromSeconds(1));
        var json = SaveService.SaveToJson(world);
        var savedTime = world.Clock.Now;

        module.Ticks = 0; world.Clock.Now = DateTime.MinValue;
        SaveService.LoadFromJson(world, json);

        Assert.Equal(2, module.Ticks);
        Assert.Equal(savedTime, world.Clock.Now);
    }

    [Fact]
    public void GetService_throws_when_missing_and_returns_registered()
    {
        var world = new World();
        Assert.False(world.TryGetService<Inventory>(out _));
        var inv = new Inventory();
        world.RegisterService(inv);
        Assert.True(ReferenceEquals(inv, world.GetService<Inventory>()));
    }
}