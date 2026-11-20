using NUnit.Framework;

using TownSystem.Map.Models;

namespace TownSystem.Map.Tests;

public class ResourceDepositTests
{
    [Test]
    public void CreateDeposit()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 1000);

        Assert.That(deposit.Amount, Is.EqualTo(1000));
        Assert.That(deposit.IsEmpty, Is.False);
    }

    [Test]
    public void ExtractResource()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 1000);

        int extracted = deposit.Extract(200);

        Assert.That(extracted, Is.EqualTo(200));
        Assert.That(deposit.Amount, Is.EqualTo(800));
    }

    [Test]
    public void ExtractMoreThanAvailable()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 100);

        int extracted = deposit.Extract(500);

        Assert.That(extracted, Is.EqualTo(100));
        Assert.That(deposit.Amount, Is.EqualTo(0));
        Assert.That(deposit.IsEmpty, Is.True);
    }

    [Test]
    public void ExtractFromEmptyDeposit()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 0);

        Assert.That(deposit.Extract(100), Is.EqualTo(0));
    }

    [Test]
    public void NegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ResourceDeposit(NaturalResource.Iron, -100));
    }
}