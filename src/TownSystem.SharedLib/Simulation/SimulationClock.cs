namespace TownSystem.SharedLib.Simulation;

/// <summary>
/// Игровые часы.
/// Здесь хранится само время, его скорость, паузу
/// </summary>
public sealed class SimulationClock
{
    /// <summary>
    /// Текущее игровое время.
    /// </summary>
    public DateTime Now { get; set; } = new(2026, 1, 1, 8, 0, 0);

    /// <summary>
    /// Во сколько раз игровое время идёт быстрее реального.
    /// </summary>
    public double Speed { get; private set; } = 60;

    /// <summary>
    /// Симуляция на паузе.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Ставит симуляцию на паузу.
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    /// Снимает время с паузы.
    /// </summary>
    public void Resume() => IsPaused = false;

    /// <summary>
    /// Устанавливает новую скорость.
    /// </summary>
    /// <param name="speed">Скорость времени</param>
    public void SetSpeed(double speed = 60) => Speed = speed;

    /// <summary>
    /// Продвигает время на реальный интервал с учётом скорости.
    /// </summary>
    /// <param name="realDelta">Интервал реального времени</param>
    /// <returns>Прошедшее игровое время</returns>
    public TimeSpan Advance(TimeSpan realDelta)
    {
        if (IsPaused || Speed <= 0) return TimeSpan.Zero;
        var delta = realDelta * Speed;
        Now += delta;
        return delta;
    }
}