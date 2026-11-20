using System.Text.Json.Nodes;

namespace TownSystem.SharedLib.Persistence;

/// <summary>
/// Модуль (или сервис), состояние которого сохраняется. Реализуется на том же классе, что и
/// <see cref="Simulation.ISimulationModule"/>. Сохранением в файл занимается <see cref="SaveService"/>.
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Уникальный ключ раздела в файле сохранения (например, "police").
    /// </summary>
    string SaveKey { get; }

    /// <summary>
    /// Возвращает состояние в виде JSON. Каждый вызов должен возвращать новое дерево узлов.
    /// </summary>
    JsonNode Save();

    /// <summary>
    /// Восстанавливает состояние из JSON, который вернул <see cref="Save"/>.
    /// </summary>
    void Load(JsonNode state);
}