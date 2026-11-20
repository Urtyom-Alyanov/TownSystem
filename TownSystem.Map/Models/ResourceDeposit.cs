using System;
using System.Collections.Generic;
using System.Text;

namespace TownSystem.Map.Models;

/// <summary>
/// Месторождение природного ресурса с определённым запасом
/// </summary>
public class ResourceDeposit
{
    public NaturalResource Resource { get; }

    public int InitialAmount { get; }

    public int Amount { get; private set; }

    public bool IsEmpty => Amount == 0;

    public ResourceDeposit(NaturalResource resource, int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Resource = resource;
        InitialAmount = amount;
        Amount = amount;
    }

    // Изъятие ресурса из месторождения
    public int Extract(int amount)
    {
        if (amount <= 0)
            return 0;

        int extracted = Math.Min(amount, Amount);

        Amount -= extracted;

        return extracted;
    }
}
