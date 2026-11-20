namespace TownSystem.SharedLib.Core;

/// <summary>
/// Базовая реализация <see cref="IEntity"/>.
/// От неё удобно наследовать сущности модулей.
/// </summary>
public abstract class EntityBase : IEntity
{
    /// <summary>
    /// Создаёт сущность с новым (или заданным, при загрузке из сохранения) идентификатором.
    /// </summary>
    /// <param name="name">Название сущности</param>
    /// <param name="id">Идентификатор</param>
    protected EntityBase(string name, EntityId? id = null)
    {
        Name = name;
        Id = id ?? EntityId.New();
    }

    /// <inheritdoc />
    public EntityId Id { get; }

    /// <inheritdoc />
    public string Name { get; protected set; }

    /// <inheritdoc />
    public override string ToString() => $"{GetType().Name} {Name} [{Id}]";
}