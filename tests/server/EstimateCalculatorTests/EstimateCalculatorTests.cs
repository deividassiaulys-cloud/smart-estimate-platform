using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.EstimateCalculatorTests;

/// <summary>
/// The estimate calculation rules (BR-015–BR-017), checked without a database against the worked example in
/// specs/007-estimate-creation-and-calculation/spec.md.
/// </summary>
public sealed class EstimateCalculatorTests
{
    // "Siena A++": 0.8 h/m² at 15 €/h, with three materials, as in the spec's worked example.
    private static EstimateWorkLine WallLine(decimal quantity) => new("Siena A++", "m²", 15m, 0.8m, quantity,
    [
        new EstimateMaterialLine("Gipso plokštė", "m²", 4.50m, 2m, 10m),
        new EstimateMaterialLine("Varžtai", "vnt.", 0.02m, 25m, 5m),
        new EstimateMaterialLine("Plėvelė", "m²", 1.10m, 1m, 5m)
    ]);

    private static Estimate EstimateWith(decimal overhead, decimal profit, decimal vat, params EstimateWorkLine[] lines)
    {
        var estimate = new Estimate(projectId: 1, "Pradinė sąmata", overhead, profit, vat);
        foreach (var line in lines)
        {
            estimate.AddWorkLine(line);
        }

        return estimate;
    }

    [Fact]
    public void WorkedExample_TotalIs1263_26()
    {
        var result = EstimateCalculator.Calculate(EstimateWith(10m, 15m, 21m, WallLine(35m)));

        var line = Assert.Single(result.WorkLines);
        Assert.Equal(420.00m, line.LabourCost);
        Assert.Equal([346.50m, 18.38m, 40.43m], line.MaterialLines.Select(m => m.Cost));
        Assert.Equal(825.31m, result.DirectCosts);
        Assert.Equal(82.53m, result.Overhead);
        Assert.Equal(136.18m, result.Profit);
        Assert.Equal(219.24m, result.Vat);
        Assert.Equal(1263.26m, result.Total);
    }

    [Fact]
    public void MidpointMaterialCost_IsRoundedAwayFromZero()
    {
        var result = EstimateCalculator.Calculate(EstimateWith(0m, 0m, 0m, WallLine(35m)));

        // 36.75 × 1.10 = 40.425: banker's rounding would give 40.42.
        Assert.Equal(40.43m, result.WorkLines[0].MaterialLines[2].Cost);
    }

    [Theory]
    [InlineData(35, 2, 10, 77)]
    [InlineData(35, 25, 5, 918.75)]
    [InlineData(35, 1, 5, 36.75)]
    [InlineData(10, 1, 0, 10)]
    public void MaterialQuantity_IsQuantityTimesNormTimesWaste(decimal quantity, decimal norm, decimal waste, decimal expected) =>
        Assert.Equal(expected, EstimateCalculator.MaterialQuantity(quantity, norm, waste));

    [Fact]
    public void QuantitiesAndNorms_AreNeverRounded()
    {
        // 1.234 × 0.333 × 1.05 = 0.4314681: more than 3 decimals, kept as is.
        var line = new EstimateWorkLine("Darbas", "m²", 10m, 0.333m, 1.234m,
            [new EstimateMaterialLine("Medžiaga", "kg", 2m, 0.333m, 5m)]);

        var result = EstimateCalculator.Calculate(EstimateWith(0m, 0m, 0m, line));

        var material = Assert.Single(result.WorkLines[0].MaterialLines);
        Assert.Equal(1.234m * 0.333m * 1.05m, material.Quantity);
        Assert.Equal(0.4314681m, material.Quantity);
        // Only money is rounded: 0.4314681 × 2 = 0.8629362 → 0.86, and 1.234 × 0.333 × 10 = 4.10922 → 4.11.
        Assert.Equal(0.86m, material.Cost);
        Assert.Equal(4.11m, result.WorkLines[0].LabourCost);
    }

    [Fact]
    public void WorkWithoutMaterials_CostsOnlyItsLabour()
    {
        var line = new EstimateWorkLine("Konsultacija", "val.", 40m, 1m, 2.5m, []);

        var result = EstimateCalculator.Calculate(EstimateWith(0m, 0m, 21m, line));

        Assert.Equal(100.00m, result.DirectCosts);
        Assert.Equal(21.00m, result.Vat);
        Assert.Equal(121.00m, result.Total);
    }

    [Fact]
    public void SameWorkAddedTwice_GivesTwoIndependentLines()
    {
        var result = EstimateCalculator.Calculate(EstimateWith(0m, 0m, 0m, WallLine(35m), WallLine(10m)));

        Assert.Equal([420.00m, 120.00m], result.WorkLines.Select(l => l.LabourCost));
        Assert.Equal(825.31m + result.WorkLines[1].LabourCost + result.WorkLines[1].MaterialLines.Sum(m => m.Cost), result.DirectCosts);
    }

    [Fact]
    public void Total_IsTheSumOfTheRoundedParts()
    {
        var result = EstimateCalculator.Calculate(EstimateWith(10m, 15m, 21m, WallLine(35m)));

        Assert.Equal(result.DirectCosts + result.Overhead + result.Profit + result.Vat, result.Total);
    }
}
