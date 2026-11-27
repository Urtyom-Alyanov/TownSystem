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

        double extracted = deposit.Extract(200);

        Assert.That(extracted, Is.EqualTo(200));
        Assert.That(deposit.Amount, Is.EqualTo(800));
    }

    // Если запросили больше чем есть, отдаём остаток
    [Test]
    public void ExtractMoreThanAvailable()
    {
        ResourceDeposit deposit = new ResourceDeposit(
            NaturalResource.Iron, 100);

        double extracted = deposit.Extract(500);

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

    // Дробный запас списывается до нуля даже если запрос больше остатка
    [Test]
    public void ExtractFractionalStock()
    {
        ResourceDeposit deposit = new ResourceDeposit(NaturalResource.Iron, 1.25);

        Assert.That(deposit.InitialAmount, Is.EqualTo(1.25));
        Assert.That(deposit.Extract(0.5), Is.EqualTo(0.5));
        Assert.That(deposit.Amount, Is.EqualTo(0.75));
        Assert.That(deposit.Extract(1), Is.EqualTo(0.75));
        Assert.That(deposit.IsEmpty, Is.True);
    }

    // Нельзя выдать ресурс если запрос меньше точности хранимого запаса
    [Test]
    public void RequestBelowStockPrecisionDoesNotExtract()
    {
        ResourceDeposit deposit = new ResourceDeposit(NaturalResource.Iron, 5);

        Assert.That(deposit.Extract(double.Epsilon), Is.Zero);
        Assert.That(deposit.Amount, Is.EqualTo(5));
    }

    // Не даём создать месторождение с некорректным запасом
    [TestCase(-100.0)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void InvalidInitialAmount(double amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ResourceDeposit(NaturalResource.Iron, amount));
    }
}