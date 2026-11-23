using TownSystem.Residential;
using TownSystem.SharedLib.Contracts;
using TownSystem.SharedLib.Core;

namespace TownSystem.Residential.Tests;

public class ResidentialBuildingTests
{
    private static Position TestPosition => new(10, 20);
    private static Footprint TestFootprint => new(2, 2);
    private static DateTime TestDate => new(2020, 1, 1);

    [Fact]
    public void Constructor_WithValidParameters_CreatesBuilding()
    {
        // Arrange & Act
        var building = new ResidentialBuilding(
            "Test House",
            ResidentialBuildingType.PrivateHouse,
            TestPosition,
            TestFootprint,
            1,
            TestDate
        );

        // Assert
        Assert.Equal("Test House", building.Name);
        Assert.Equal(ResidentialBuildingType.PrivateHouse, building.BuildingType);
        Assert.Equal(TestPosition, building.Position);
        Assert.Equal(TestFootprint, building.Footprint);
        Assert.Equal(1u, building.Floors);
        Assert.Equal(TestDate, building.ConstructionDate);
        Assert.Equal(0.0, building.Wear);
        Assert.Equal(BuildingCondition.New, building.Condition);
    }

    [Fact]
    public void Constructor_WithZeroFloors_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new ResidentialBuilding("Bad House", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 0, TestDate)
        );
    }

    [Theory]
    [InlineData(ResidentialBuildingType.PrivateHouse, 1u, 4u)]
    [InlineData(ResidentialBuildingType.LowRiseApartment, 3u, 24u)]
    [InlineData(ResidentialBuildingType.MidRiseApartment, 7u, 84u)]
    [InlineData(ResidentialBuildingType.HighRiseApartment, 10u, 150u)]
    [InlineData(ResidentialBuildingType.Dormitory, 5u, 100u)]
    public void Capacity_CalculatedCorrectly(ResidentialBuildingType type, uint floors, uint expectedCapacity)
    {
        // Arrange
        var building = new ResidentialBuilding("Test", type, TestPosition, TestFootprint, floors, TestDate);

        // Act & Assert
        Assert.Equal(expectedCapacity, building.Capacity);
    }

    [Fact]
    public void Category_AlwaysResidential()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);

        // Act & Assert
        Assert.Equal(BuildingCategory.Residential, building.Category);
    }

    [Fact]
    public void Wear_ClampsTo01Range()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);

        // Act
        building.AccumulateWear(TimeSpan.FromDays(1_000_000)); // Огромный износ

        // Assert
        Assert.Equal(1.0, building.Wear);
    }

    [Theory]
    [InlineData(0.0, BuildingCondition.New)]
    [InlineData(0.15, BuildingCondition.New)]
    [InlineData(0.3, BuildingCondition.Good)]
    [InlineData(0.5, BuildingCondition.Fair)]
    [InlineData(0.7, BuildingCondition.NeedsRepair)]
    [InlineData(0.9, BuildingCondition.Poor)]
    [InlineData(0.98, BuildingCondition.Critical)]
    public void Condition_BasedOnWear(double wear, BuildingCondition expectedCondition)
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);
        building.AccumulateWear(TimeSpan.FromDays(wear * 100_000)); // Имитируем определённый уровень износа

        // Act & Assert
        Assert.Equal(expectedCondition, building.Condition);
    }

    [Fact]
    public void Maintain_WithRepairType_ReducesWear()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);
        building.AccumulateWear(TimeSpan.FromDays(10_000)); // Добавляем износ
        double wearBefore = building.Wear;

        // Act
        double wearReduced = building.Maintain(MaintenanceType.Repair, 1000);

        // Assert
        Assert.Equal(0.05, wearReduced);
        Assert.Equal(wearBefore - 0.05, building.Wear);
    }

    [Fact]
    public void Maintain_RecordsHistory()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);

        // Act
        building.Maintain(MaintenanceType.Inspection, 500);
        building.Maintain(MaintenanceType.Repair, 1000);

        // Assert
        Assert.Equal(2, building.MaintenanceHistory.Count);
        Assert.Equal(MaintenanceType.Inspection, building.MaintenanceHistory[0].Type);
        Assert.Equal(MaintenanceType.Repair, building.MaintenanceHistory[1].Type);
    }

    [Fact]
    public void TotalMaintenanceCost_SumsAllCosts()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);

        // Act
        building.Maintain(MaintenanceType.Inspection, 500);
        building.Maintain(MaintenanceType.Repair, 1000);
        building.Maintain(MaintenanceType.MajorRepair, 5000);

        // Assert
        Assert.Equal(6500, building.TotalMaintenanceCost);
    }

    [Fact]
    public void LastMaintenanceDate_ReturnsNullWhenNoMaintenance()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);

        // Act & Assert
        Assert.Null(building.LastMaintenanceDate);
    }

    [Fact]
    public void LastMaintenanceDate_ReturnsLastDate()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);
        var date1 = new DateTime(2026, 1, 1);
        var date2 = new DateTime(2026, 6, 1);

        // Act
        building.Maintain(MaintenanceType.Repair, 1000, date1);
        building.Maintain(MaintenanceType.MajorRepair, 5000, date2);

        // Assert
        Assert.Equal(date2, building.LastMaintenanceDate);
    }

    [Fact]
    public void AccumulateWear_IncreasesSmoothly()
    {
        // Arrange
        var building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 1, TestDate);

        // Act
        building.AccumulateWear(TimeSpan.FromDays(1));

        // Assert
        Assert.True(building.Wear > 0);
        Assert.True(building.Wear < 0.001);
    }

    [Fact]
    public void IBuilding_PropertiesExposed()
    {
        // Arrange & Act
        IBuilding building = new ResidentialBuilding("Test", ResidentialBuildingType.PrivateHouse, TestPosition, TestFootprint, 2, TestDate);

        // Assert
        Assert.Equal(BuildingCategory.Residential, building.Category);
        Assert.Equal(TestPosition, building.Position);
        Assert.Equal(TestFootprint, building.Footprint);
        Assert.Equal(2u, building.Floors);
        Assert.Equal(8u, building.Capacity); // PrivateHouse: 4 * 2 = 8
        Assert.Equal(0.0, building.Wear);
    }
}
