using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Simulation;

namespace TownSystem.SharedLib.Emergencies;

/// <summary>
/// Модуль управления чрезвычайными ситуациями.
/// </summary>
public sealed class EmergencyModule : ISimulationModule
{
    public string Name => "Emergency Management";

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var service = new EmergencyManagementService(
            world.Events);

        world.RegisterService<IEmergencyManagementService>(
            service);
    }

    /// <inheritdoc />
    public void Tick(TickContext context)
    {
    }
}