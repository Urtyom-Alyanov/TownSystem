using System.Text.Json;
using System.Text.Json.Nodes;

using TownSystem.SharedLib.Simulation;

namespace TownSystem.SharedLib.Persistence;

/// <summary>
/// Сохраняет и загружает мир.
/// Собирает состояние всех модулей, реализующих <see cref="ISaveable"/>.
/// </summary>
public static class SaveService
{
    /// <summary>
    /// Версия формата файла. Увеличивать при несовместимых изменениях.
    /// </summary>
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>
    /// Сериализует мир в JSON-строку.
    /// </summary>
    /// <param name="world">Симуляция</param>
    /// <returns>JSON-строка с сохранением</returns>
    public static string SaveToJson(IWorld world)
    {
        var sections = new JsonObject();
        foreach (var saveable in world.Modules.OfType<ISaveable>())
        {
            if (sections.ContainsKey(saveable.SaveKey))
                throw new InvalidOperationException($"Повторяющийся SaveKey '{saveable.SaveKey}'.");
            sections[saveable.SaveKey] = saveable.Save();
        }

        var root = new JsonObject
        {
            ["version"] = CurrentVersion,
            ["simTime"] = world.Clock.Now,
            ["modules"] = sections
        };
        return root.ToJsonString(Options);
    }

    /// <summary>
    /// Восстанавливает мир из JSON.
    /// Модули без раздела в файле остаются как есть.
    /// </summary>
    /// <param name="world">Симуляция (происходит мутация над этим объектом!)</param>
    /// <param name="json">Сохранение в виде JSON</param>
    public static void LoadFromJson(IWorld world, string json)
    {
        var root = JsonNode.Parse(json)?.AsObject() ?? throw new InvalidDataException("Пустой файл сохранения.");
        var version = root["version"]?.GetValue<int>() ?? 0;
        if (version != CurrentVersion)
            throw new InvalidDataException($"Неподдерживаемая версия сохранения: {version} (нужна {CurrentVersion}).");

        world.Clock.Now = root["simTime"]!.GetValue<DateTime>();
        var sections = root["modules"]!.AsObject();
        foreach (var saveable in world.Modules.OfType<ISaveable>())
            if (sections[saveable.SaveKey] is { } state)
                saveable.Load(state);
    }

    /// <summary>
    /// Сохраняет мир в файл.
    /// </summary>
    /// <param name="world">Симуляция</param>
    /// <param name="path">Путь до файла</param>
    public static void SaveToFile(IWorld world, string path) => File.WriteAllText(path, SaveToJson(world));

    /// <summary>
    /// Загружает мир из файла.
    /// </summary>
    /// <param name="world">Симуляция (происходит мутация над этим объектом!)</param>
    /// <param name="path">Путь до файла</param>
    public static void LoadFromFile(IWorld world, string path) => LoadFromJson(world, File.ReadAllText(path));
}