namespace TownSystem.SharedLib.Core;

/// <summary>
/// Это склад. Здесь хранится количество предметов каждого вида.
/// Он общий для добычи, логистики, заводов, магазинов и т.п..
///
/// Здесь он нужен для тогоЮ чтобы груз можно было передавать
/// меж модулями
/// </summary>
/// <seealso cref="ItemType"/>
/// <param name="capacity">Вместимость склада</param>
public sealed class Inventory(double? capacity = null)
{
    private readonly Dictionary<ItemType, double> _items = new();

    /// <summary>
    /// Максимальная вместимость.
    ///
    /// null, если ограничения нет.
    /// </summary>
    public double? Capacity { get; } = capacity;

    /// <summary>
    /// Сколько всего лежит на складе предметов.
    /// </summary>
    public double Total => _items.Values.Sum();

    /// <summary>
    /// Содержимое склада
    /// </summary>
    public IReadOnlyDictionary<ItemType, double> Items => _items;

    /// <summary>
    /// Количество предметов указанного типа
    /// </summary>
    /// <param name="type">Тип</param>
    /// <returns>Количество</returns>
    public double CountOf(ItemType type) => _items.GetValueOrDefault(type);

    /// <summary>
    /// Добавляет в склад предмет <paramref name="type"/>
    /// количеством <paramref name="amount"/>
    /// </summary>
    /// <param name="type">Тип предмета</param>
    /// <param name="amount">Количество предмета</param>
    /// <returns>Количество принятого предмета</returns>
    public double Add(ItemType type, double amount)
    {
        if (amount <= 0) return 0;
        var free = Capacity is { } cap ? Math.Max(0, cap - Total) : double.MaxValue;
        var accepted = Math.Min(amount, free);
        if (accepted > 0) _items[type] = CountOf(type) + accepted;
        return accepted;
    }

    /// <summary>
    /// Удаляет n-ное количество <paramref name="type"/> со склада,
    /// только если предмета меньше, чем <paramref name="amount"/>.
    /// </summary>
    /// <param name="type">Тип предмета</param>
    /// <param name="amount">Количество предмета</param>
    /// <returns></returns>
    public bool TryRemove(ItemType type, double amount)
    {
        if (amount < 0 || CountOf(type) < amount) return false;
        var left = CountOf(type) - amount;
        if (left <= 1e-9) _items.Remove(type); else _items[type] = left;
        return true;
    }

    /// <summary>
    /// Превращает в словарь код предмета-количество
    /// </summary>
    /// <returns>Словарь</returns>
    public Dictionary<string, double> ToDictionary() =>
        _items.ToDictionary(
            p => p.Key.Code,
            p => p.Value
        );

    /// <summary>
    /// Восстанавливает содержимое из словаря код предмета-количество
    /// (которое можно получить с помощью <see cref="ToDictionary"/>).
    ///
    /// Примечание: старые данные, что лежали до загрузки, будут удалены.
    /// </summary>
    /// <param name="data">Словарь код-количество</param>
    public void LoadFrom(IReadOnlyDictionary<string, double> data)
    {
        _items.Clear();
        foreach (var (code, amount) in data) _items[new ItemType(code)] = amount;
    }
}