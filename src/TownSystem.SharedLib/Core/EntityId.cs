namespace TownSystem.SharedLib.Core;

/// <summary>
/// Уникальный идентификатор любой сущности города.
/// </summary>
/// <param name="Value">Значение идентификатора, рекомендуется пользоваться <see cref="New"/></param>
public readonly record struct EntityId(Guid Value)
{
    /// <summary>
    /// Создаёт новый случайный идентификатор.
    /// </summary>
    public static EntityId New() => new(Guid.NewGuid());

    /// <summary>
    /// Краткая запись для отладки и интерфейса.
    /// </summary>
    public override string ToString() => Value.ToString("N")[..8];
}
