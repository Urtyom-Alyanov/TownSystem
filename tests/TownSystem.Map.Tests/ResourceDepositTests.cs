using NUnit.Framework;

using TownSystem.Map.Models;

namespace TownSystem.Map.Tests;

/// <summary>
/// тесты на запасы месторождения и на то, как они меняются после добычи
/// </summary>
public class ResourceDepositTests
{
    // Смотрим сколько ресурса лежит в новом месторождении
    [Test]
    public void CreateDeposit()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 1000);

        Assert.That(deposit.Amount, Is.EqualTo(1000));
        Assert.That(deposit.IsEmpty, Is.False);
    }

    // после добычи должно остаться ровно столько, сколько не забрали
    [Test]
    public void ExtractResource()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 1000);

        int extracted = deposit.Extract(200);

        Assert.That(extracted, Is.EqualTo(200));
        Assert.That(deposit.Amount, Is.EqualTo(800));
    }

    // Если запросили больше чем есть, отдаём остаток
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

    // из пустого месторождения уже нечего брать
    [Test]
    public void ExtractFromEmptyDeposit()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 0);

        Assert.That(deposit.Extract(100), Is.EqualTo(0));
    }

    // с минусовым запасом создать месторождение нельзя
    [Test]
    public void NegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ResourceDeposit(NaturalResource.Iron, -100));
    }
}