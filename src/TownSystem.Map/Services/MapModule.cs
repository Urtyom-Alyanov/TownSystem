using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Simulation;

using MapModel = TownSystem.Map.Models.Map;

namespace TownSystem.Map.Services;

/// <summary>
/// Подключает карту к миру, чтобы другие модули могли запросить её сервис
/// </summary>
public sealed class MapModule : ISimulationModule
{
    public string Name => "Map";

    public int Order => -100;

    public MapModel Map { get; }

    // Принимаем карту, которую можно также передать в её интерфейс
    public MapModule(MapModel map)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
    }

    // Публикуем сервис карты при регистрации модуля в мире
    public void Initialize(IWorld world)
    {
        world.RegisterService<IMapService>(new MapService(Map));
    }

    // Карта меняется по запросам модулей, сама за такт ничего не делает
    public void Tick(TickContext context)
    {
    }
}
