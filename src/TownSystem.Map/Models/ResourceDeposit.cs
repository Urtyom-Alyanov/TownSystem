namespace TownSystem.Map.Models;

/// <summary>
/// Тип ресурса и сколько его было сначала и осталось сейчас
/// </summary>
public class ResourceDeposit
{
    public NaturalResource Resource { get; }

    public double InitialAmount { get; }

    public double Amount { get; private set; }

    public bool IsEmpty => Amount == 0;

    // Запоминаем какой это ресурс и сколько его изначально было
    public ResourceDeposit(NaturalResource resource, double amount)
    {
        if (!double.IsFinite(amount) || amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Resource = resource;
        InitialAmount = amount;
        Amount = amount;
    }

    // Берём сколько запросили или сколько осталось, если запаса меньше
    public double Extract(double amount)
    {
        if (!double.IsFinite(amount) || amount <= 0)
            return 0;

        double extracted = Math.Min(amount, Amount);
        double remaining = Amount - extracted;

        // Если запрос слишком мал для точности числа, запас не изменился
        if (remaining == Amount)
            return 0;

        Amount = remaining;
        return extracted;
    }
}
