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
        new EstimateMaterialLine("Gipso plokštė", "m²", 4.50m, 2m, 10m, MaterialKind.Material),
        new EstimateMaterialLine("Varžtai", "vnt.", 0.02m, 25m, 5m, MaterialKind.Material),
        new EstimateMaterialLine("Plėvelė", "m²", 1.10m, 1m, 5m, MaterialKind.Material)
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

    // ---- Feature 017: Labour / Materials / Machinery, hours and unit price ----

    [Fact]
    public void WorkedExample_IsSplitIntoLabourMaterialsAndMachinery()
    {
        var result = EstimateCalculator.Calculate(EstimateWith(10m, 15m, 21m, WallLine(35m)));

        var line = Assert.Single(result.WorkLines);
        Assert.Equal((28m, 420.00m, 405.31m, 0m, 825.31m, 23.58m),
            (line.Hours, line.LabourCost, line.Materials, line.Machinery, line.Amount, line.UnitPrice));
        Assert.Equal((420.00m, 405.31m, 0m, 825.31m), (result.Labour, result.Materials, result.Machinery, result.DirectCosts));
        Assert.Equal(1263.26m, result.Total);
    }

    [Fact]
    public void MachineryLine_IsCountedUnderMachinery()
    {
        // Spec 017 US2-2: "Įrankiai ir įrengimai" (Machinery, norm 1, waste 0, 0,20 €) added to the wall.
        var wall = WallLine(35m);
        wall.MaterialLines.Add(new EstimateMaterialLine("Įrankiai ir įrengimai", "vnt.", 0.20m, 1m, 0m, MaterialKind.Machinery));

        var result = EstimateCalculator.Calculate(EstimateWith(10m, 15m, 21m, wall));

        var line = Assert.Single(result.WorkLines);
        Assert.Equal((420.00m, 405.31m, 7.00m, 832.31m, 23.78m),
            (line.LabourCost, line.Materials, line.Machinery, line.Amount, line.UnitPrice));
        Assert.Equal((832.31m, 83.23m, 137.33m, 221.10m, 1273.97m),
            (result.DirectCosts, result.Overhead, result.Profit, result.Vat, result.Total));
    }

    [Fact]
    public void WorkLineWithoutMaterialsOrLabour_HasZeroParts()
    {
        var noMaterials = new EstimateWorkLine("Montavimas", "val.", 30m, 1m, 2m, []);
        var noLabour = new EstimateWorkLine("Pristatymas", "kompl.", 0m, 0m, 1m,
            [new EstimateMaterialLine("Transportas", "kompl.", 50m, 1m, 0m, MaterialKind.Machinery)]);

        var result = EstimateCalculator.Calculate(EstimateWith(0m, 0m, 0m, noMaterials, noLabour));

        Assert.Equal((60m, 0m, 0m, 60m, 30m),
            (result.WorkLines[0].LabourCost, result.WorkLines[0].Materials, result.WorkLines[0].Machinery,
                result.WorkLines[0].Amount, result.WorkLines[0].UnitPrice));
        Assert.Equal((0m, 0m, 50m, 50m),
            (result.WorkLines[1].LabourCost, result.WorkLines[1].Materials, result.WorkLines[1].Machinery, result.WorkLines[1].Amount));
    }

    [Fact]
    public void EstimateParts_AddUpToTheDirectCostsExactly()
    {
        var wall = WallLine(35m);
        wall.MaterialLines.Add(new EstimateMaterialLine("Įrankiai ir įrengimai", "vnt.", 0.20m, 1m, 0m, MaterialKind.Machinery));

        var result = EstimateCalculator.Calculate(EstimateWith(10m, 15m, 21m, wall, WallLine(12.345m)));

        Assert.Equal(result.DirectCosts, result.Labour + result.Materials + result.Machinery);
        Assert.Equal(result.DirectCosts, result.WorkLines.Sum(l => l.Amount));
        Assert.All(result.WorkLines, l => Assert.Equal(l.Amount, l.LabourCost + l.Materials + l.Machinery));
    }

    [Fact]
    public void PrecisePricesAndNorms_AreUsedUnroundedAndOnlyAmountsAreRounded()
    {
        // Feature 016 US2-1: "Tašelis 28x45 mm", 121,7 m², labour 25,00 €/h × 0,042002 h; screws 5 × 0,1000 €.
        var batten = new EstimateWorkLine("Tašelis 28x45 mm", "m²", 25.00m, 0.042002m, 121.7m,
            [new EstimateMaterialLine("Medsraigčiai", "vnt.", 0.1000m, 5m, 0m, MaterialKind.Material)]);
        // US2-2: a calculated quantity of 1 234,5 vnt. at 0,0616 €.
        var screws = new EstimateWorkLine("Terasa", "m²", 0m, 0m, 1234.5m,
            [new EstimateMaterialLine("Sraigtai terasoms", "vnt.", 0.0616m, 1m, 0m, MaterialKind.Material)]);

        var result = EstimateCalculator.Calculate(EstimateWith(0m, 0m, 0m, batten, screws));

        Assert.Equal(127.79m, result.WorkLines[0].LabourCost); // 127,791085
        Assert.Equal(608.5m, result.WorkLines[0].MaterialLines[0].Quantity);
        Assert.Equal(60.85m, result.WorkLines[0].MaterialLines[0].Cost);
        Assert.Equal(76.05m, result.WorkLines[1].MaterialLines[0].Cost); // 76,0452
        Assert.Equal(127.79m + 60.85m + 76.05m, result.Total);
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
            [new EstimateMaterialLine("Medžiaga", "kg", 2m, 0.333m, 5m, MaterialKind.Material)]);

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
