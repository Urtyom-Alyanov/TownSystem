namespace TownSystem.SharedLib.Simulation;

/// <summary>
/// Данные одного такта симуляции.
/// </summary>
/// <param name="Delta">
/// Сколько игрового времени прошло с прошлого такта (уже с учётом скорости).
/// Получать с помощью <see cref="SimulationClock.Advance"/>
/// </param>
/// <param name="Now">Текущее игровое время после такта.</param>
/// <seealso cref="SimulationClock" />
public readonly record struct TickContext(TimeSpan Delta, DateTime Now);