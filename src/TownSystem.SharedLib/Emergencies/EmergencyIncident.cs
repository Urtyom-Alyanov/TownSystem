using TownSystem.SharedLib.Core;
using TownSystem.SharedLib.Events;

namespace TownSystem.SharedLib.Emergencies;

/// <summary>
/// Модель чрезвычайного происшествия в городе.
/// </summary>
public sealed class EmergencyIncident : EntityBase
{
    public EmergencyKind Kind { get; }

    public Position Location { get; }

    /// <summary>
    /// Уровень тяжести происшествия от 1 до 5.
    /// </summary>
    public int Severity { get; }

    public EntityId? SubjectId { get; }

    public DateTimeOffset CreatedAt { get; }

    public EmergencyStatus Status { get; private set; }
        = EmergencyStatus.Reported;

    public EmergencyIncident(
        string name,
        EmergencyKind kind,
        Position location,
        int severity,
        EntityId? subjectId = null,
        EntityId? id = null,
        DateTimeOffset? createdAt = null)
        : base(ValidateName(name), id)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));

        if (severity is < 1 or > 5)
            throw new ArgumentOutOfRangeException(
                nameof(severity),
                "Уровень тяжести должен быть от 1 до 5.");

        Kind = kind;
        Location = location;
        Severity = severity;
        SubjectId = subjectId;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    public void StartResponse()
    {
        EnsureStatus(EmergencyStatus.Reported);
        Status = EmergencyStatus.InProgress;
    }
    public void MarkResolved()
    {
        EnsureStatus(EmergencyStatus.InProgress);
        Status = EmergencyStatus.Resolved;
    }
    public void Cancel()
    {
        if (Status is not (
            EmergencyStatus.Reported or
            EmergencyStatus.InProgress))
        {
            throw new InvalidOperationException(
                "Нельзя отменить завершённое происшествие.");
        }

        Status = EmergencyStatus.Cancelled;
    }
    private void EnsureStatus(EmergencyStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(
                $"Ожидался статус {expected}, текущий статус: {Status}.");
        }
    }
    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}