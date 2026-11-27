using TownSystem.SharedLib.Core;
namespace TownSystem.FireService.Models;

public enum FirefighterRank
{
    Firefighter,
    SeniorFirefighter,
    SquadLeader,
    ShiftSupervisor
}

/// <summary>Сотрудник пожарно-спасательной службы.</summary>
public sealed class Firefighter : EntityBase
{
    public Firefighter(
        string name,
        FirefighterRank rank = FirefighterRank.Firefighter,
        int experienceYears = 0,
        bool isOnDuty = true,
        EntityId? id = null)
        : base(name, id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("У сотрудника должно быть имя.", nameof(name));

        if (experienceYears < 0)
            throw new ArgumentOutOfRangeException(nameof(experienceYears));

        if (!Enum.IsDefined(rank))
            throw new ArgumentOutOfRangeException(nameof(rank));

        Rank = rank;
        ExperienceYears = experienceYears;
        IsOnDuty = isOnDuty;
        IsAvailable = isOnDuty;
    }

    public FirefighterRank Rank { get; }

    public string RankDisplayName => Rank switch
    {
        FirefighterRank.Firefighter => "Пожарный",
        FirefighterRank.SeniorFirefighter => "Старший пожарный",
        FirefighterRank.SquadLeader => "Командир отделения",
        FirefighterRank.ShiftSupervisor => "Начальник караула",
        _ => Rank.ToString()
    };

    public int ExperienceYears { get; }

    public bool IsOnDuty { get; }

    public bool IsAvailable { get; private set; }

    internal void SetDispatched()
    {
        IsAvailable = false;
    }

    internal void SetAvailable()
    {
        IsAvailable = IsOnDuty;
    }
}