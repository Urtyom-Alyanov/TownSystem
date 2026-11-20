namespace TownSystem.Map.Models;

/// <summary>
/// Тип ресурса и сколько его было сначала и осталось сейчас
/// </summary>
public class ResourceDeposit
{
    public NaturalResource Resource { get; }

    public int InitialAmount { get; }

    public int Amount { get; private set; }

    public bool IsEmpty => Amount == 0;

    // Запоминаем какой это ресурс и сколько его изначально было
    public ResourceDeposit(NaturalResource resource, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

        Resource = resource;
        InitialAmount = amount;
        Amount = amount;
    }

    // берём сколько получилось добыть и вычитаем это из запаса
    public int Extract(int amount)
    {
        if (amount <= 0)
            return 0;

        int extracted = Math.Min(amount, Amount);

        Amount -= extracted;

        return extracted;
    }
}
