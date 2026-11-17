using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.EstimateRefreshTests;

/// <summary>
/// The matching rules of a catalog refresh (spec 015), on the worked example built in memory:
/// "Siena A++" (m², 15 €/h, 0.8 h) with Gipso plokštė (4.50, norm 2, waste 10), Varžtai (0.02, 25, 5), Plėvelė (1.10, 1, 5).
/// </summary>
public sealed class EstimateCatalogRefreshTests
{
    private static Estimate WorkedExample()
    {
        var estimate = new Estimate(projectId: 1, "Pradinė sąmata", 10m, 15m, 21m);
        estimate.AddWorkLine(new EstimateWorkLine("Siena A++", "m²", 15m, 0.8m, 35m,
        [
            new EstimateMaterialLine("Gipso plokštė", "m²", 4.50m, 2m, 10m),
            new EstimateMaterialLine("Varžtai", "vnt.", 0.02m, 25m, 5m),
            new EstimateMaterialLine("Plėvelė", "m²", 1.10m, 1m, 5m)
        ]));
        return estimate;
    }

    private static readonly CatalogMaterial[] SameMaterials =
    [
        new("Gipso plokštė", "m²", 4.50m),
        new("Varžtai", "vnt.", 0.02m),
        new("Plėvelė", "m²", 1.10m)
    ];

    private static CatalogWork Wall(decimal rate = 15m, decimal norm = 0.8m, params CatalogComponent[] components) =>
        new("Siena A++", "m²", rate, norm, components.Length > 0 ? components :
        [
            new("Gipso plokštė", "m²", 2m, 10m),
            new("Varžtai", "vnt.", 25m, 5m),
            new("Plėvelė", "m²", 1m, 5m)
        ]);

    [Fact]
    public void EqualCatalog_ChangesNothing()
    {
        var result = EstimateCatalogRefresh.Apply(WorkedExample(), [Wall()], SameMaterials);

        Assert.Empty(result.Changes);
        Assert.Empty(result.NotFound);
    }

    [Fact]
    public void ChangedValues_AreAppliedAndReported()
    {
        var estimate = WorkedExample();
        CatalogMaterial[] materials = [new("Gipso plokštė", "m²", 5.00m), SameMaterials[1], SameMaterials[2]];
        var wall = Wall(16m, 0.9m,
            new("Gipso plokštė", "m²", 2.1m, 12m), new("Varžtai", "vnt.", 25m, 5m), new("Plėvelė", "m²", 1m, 5m));

        var result = EstimateCatalogRefresh.Apply(estimate, [wall], materials);

        Assert.Equal(
        [
            new RefreshChange("Siena A++", null, RefreshedField.LabourRate, 15m, 16m),
            new RefreshChange("Siena A++", null, RefreshedField.LabourNorm, 0.8m, 0.9m),
            new RefreshChange("Siena A++", "Gipso plokštė", RefreshedField.Price, 4.50m, 5.00m),
            new RefreshChange("Siena A++", "Gipso plokštė", RefreshedField.ConsumptionNorm, 2m, 2.1m),
            new RefreshChange("Siena A++", "Gipso plokštė", RefreshedField.WastePercent, 10m, 12m)
        ], result.Changes);
        var line = estimate.WorkLines[0];
        var board = line.MaterialLines[0];
        Assert.Equal((16m, 0.9m, 5.00m, 2.1m, 12m), (line.LabourRate, line.LabourNorm, board.Price, board.ConsumptionNorm, board.WastePercent));
    }

    [Fact]
    public void QuantitiesPercentagesAndLines_NeverChange()
    {
        var estimate = WorkedExample();

        EstimateCatalogRefresh.Apply(estimate, [Wall(99m)], [new("Gipso plokštė", "m²", 9m)]);

        Assert.Equal((35m, 10m, 15m, 21m), (estimate.WorkLines[0].Quantity, estimate.OverheadPercent, estimate.ProfitPercent, estimate.VatPercent));
        Assert.Single(estimate.WorkLines);
        Assert.Equal(3, estimate.WorkLines[0].MaterialLines.Count);
    }

    [Fact]
    public void MissingWorkOrMaterial_IsReportedAndLeftUnchanged_ButMaterialsOfAMissingWorkTakeTheirPrice()
    {
        var estimate = WorkedExample();
        // "Siena A++" was renamed and "Plėvelė" deleted; "Gipso plokštė" costs 5.00 now.
        CatalogMaterial[] materials = [new("Gipso plokštė", "m²", 5.00m), SameMaterials[1]];

        var result = EstimateCatalogRefresh.Apply(estimate, [Wall() with { Name = "Siena A+++" }], materials);

        Assert.Equal([new RefreshMiss("Siena A++", null), new RefreshMiss("Siena A++", "Plėvelė")], result.NotFound);
        Assert.Equal([new RefreshChange("Siena A++", "Gipso plokštė", RefreshedField.Price, 4.50m, 5.00m)], result.Changes);
        var line = estimate.WorkLines[0];
        Assert.Equal((15m, 0.8m, 1.10m), (line.LabourRate, line.LabourNorm, line.MaterialLines[2].Price));
    }

    [Fact]
    public void NamesDifferingOnlyInCase_StillMatch()
    {
        var result = EstimateCatalogRefresh.Apply(WorkedExample(), [Wall(16m) with { Name = "SIENA a++" }],
            [new("gipso PLOKŠTĖ", "m²", 5.00m), SameMaterials[1], SameMaterials[2]]);

        Assert.Empty(result.NotFound);
        Assert.Equal([RefreshedField.LabourRate, RefreshedField.Price], result.Changes.Select(c => c.Field));
    }

    [Fact]
    public void SameNameInAnotherUnit_DoesNotMatch()
    {
        var result = EstimateCatalogRefresh.Apply(WorkedExample(), [Wall() with { Unit = "m" }], SameMaterials);

        Assert.Equal([new RefreshMiss("Siena A++", null)], result.NotFound);
    }

    [Fact]
    public void MaterialListedTwice_IsMatchedInOrder()
    {
        var estimate = new Estimate(projectId: 1, "Dažymas", 0m, 0m, 21m);
        estimate.AddWorkLine(new EstimateWorkLine("Sienų dažymas", "m²", 18.5m, 0.25m, 10m,
        [
            new EstimateMaterialLine("Dažai", "l", 8m, 0.15m, 5m),
            new EstimateMaterialLine("Dažai", "l", 8m, 0.05m, 0m)
        ]));
        var painting = new CatalogWork("Sienų dažymas", "m²", 18.5m, 0.25m,
            [new("Dažai", "l", 0.2m, 5m), new("Dažai", "l", 0.1m, 0m)]);

        EstimateCatalogRefresh.Apply(estimate, [painting], [new("Dažai", "l", 8m)]);

        var lines = estimate.WorkLines[0].MaterialLines;
        Assert.Equal((0.2m, 0.1m), (lines[0].ConsumptionNorm, lines[1].ConsumptionNorm));
    }

    [Fact]
    public void ReplacedMaterialOutsideTheComposition_TakesItsPriceButKeepsItsNorms()
    {
        var estimate = WorkedExample();
        estimate.WorkLines[0].MaterialLines[0].ReplaceMaterial("Drėgmei atspari plokštė", "m²", 6.20m);
        CatalogMaterial[] materials = [new("Drėgmei atspari plokštė", "m²", 6.50m), .. SameMaterials];

        var result = EstimateCatalogRefresh.Apply(estimate, [Wall()], materials);

        Assert.Equal([new RefreshChange("Siena A++", "Drėgmei atspari plokštė", RefreshedField.Price, 6.20m, 6.50m)], result.Changes);
        var replaced = estimate.WorkLines[0].MaterialLines[0];
        Assert.Equal((2m, 10m), (replaced.ConsumptionNorm, replaced.WastePercent));
    }
}
