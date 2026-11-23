namespace TownSystem.SharedLib.Emergencies;

/// <summary>
/// Текущий статус чрезвычайного происшествия.
/// </summary>
public enum EmergencyStatus
{
    Reported,
    InProgress,
    Resolved,
    Cancelled
}