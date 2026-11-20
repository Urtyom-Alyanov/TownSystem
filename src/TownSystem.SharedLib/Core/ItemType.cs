namespace TownSystem.SharedLib.Core;

/// <summary>
/// Вид ресурса, материала или товара. Строковый код, а не enum, чтобы любой модуль мог
/// добавить свой вид без правок SharedLib (достаточно <c>new ItemType("my_item")</c>).
/// 
/// Общие виды вынесены сюда, чтобы цепочка добыча -> переработка -> цех использовала одни и те же коды.
/// </summary>
public readonly record struct ItemType(string Code)
{
    // Сырьё (карта, добыча)
    public static readonly ItemType IronOre = new("iron_ore");
    public static readonly ItemType CopperOre = new("copper_ore");
    public static readonly ItemType Oil = new("oil");
    public static readonly ItemType Gas = new("gas");
    public static readonly ItemType Wood = new("wood");

    // Материалы (переработка)
    public static readonly ItemType Steel = new("steel");
    public static readonly ItemType Copper = new("copper");
    public static readonly ItemType Fuel = new("fuel");
    public static readonly ItemType Lumber = new("lumber");

    // Продукция
    public static readonly ItemType Car = new("car");

    /// <inheritdoc />
    public override string ToString() => Code;
}