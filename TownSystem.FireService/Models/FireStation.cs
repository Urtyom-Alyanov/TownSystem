using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.FireService.Models;

/// <summary>
/// Пожарная станция — здание пожарно-спасательной службы.
/// </summary>
public sealed class FireStation : EntityBase, IBuilding
{
    public FireStation(
        string name,
        Position position,
        Footprint footprint,
        uint capacity,
        uint floors = 1,
        double wear = 0,
        EntityId? id = null)
        : base(name, id)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "У пожарной станции должно быть название.",
                nameof(name));

        if (!double.IsFinite(position.X) ||
            !double.IsFinite(position.Y) ||
            position.X < 0 ||
            position.Y < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Координаты станции должны быть конечными и неотрицательными.");
        }

        if (footprint.Width == 0 || footprint.Height == 0)
            throw new ArgumentException(
                "Размер пожарной станции должен быть больше нуля.",
                nameof(footprint));

        if (capacity == 0)
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Вместимость станции должна быть больше нуля.");

        if (floors == 0)
            throw new ArgumentOutOfRangeException(
                nameof(floors),
                "У станции должен быть хотя бы один этаж.");

        if (!double.IsFinite(wear) || wear < 0 || wear > 1)
            throw new ArgumentOutOfRangeException(
                nameof(wear),
                "Износ должен находиться в диапазоне от 0 до 1.");

        Position = position;
        Footprint = footprint;
        Capacity = capacity;
        Floors = floors;
        Wear = wear;
    }

    public BuildingCategory Category => BuildingCategory.Emergency;

    public Position Position { get; }

    public Footprint Footprint { get; }

    public uint Floors { get; }

    /// <summary>
    /// Максимальное количество сотрудников, которое может разместить станция.
    /// </summary>
    public uint Capacity { get; }

    /// <summary>
    /// Износ станции от 0 (новая) до 1 (аварийная).
    /// </summary>
    public double Wear { get; private set; }

    /// <summary>
    /// Показывает, может ли станция обслуживать пожарные команды.
    /// При максимальном износе станция неработоспособна.
    /// </summary>
    public bool IsOperational => Wear < 1;

    /// <summary>
    /// Увеличивает износ станции.
    /// </summary>
    public void ApplyDamage(double amount)
    {
        if (!double.IsFinite(amount) || amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Wear = Math.Min(1, Wear + amount);
    }

    /// <summary>
    /// Уменьшает износ станции после ремонта.
    /// </summary>
    public void Repair(double amount)
    {
        if (!double.IsFinite(amount) || amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Wear = Math.Max(0, Wear - amount);
    }
}
