namespace TownSystem.SharedLib.Core;

/// <summary>
/// Любой объект в городе.
///
/// Сюда включаются и здания и даже прохожие.
/// </summary>
public interface IEntity
{
    /// <summary>Уникальный идентификатор.</summary>
    EntityId Id { get; }

    /// <summary>Отображаемое название.</summary>
    string Name { get; }
}