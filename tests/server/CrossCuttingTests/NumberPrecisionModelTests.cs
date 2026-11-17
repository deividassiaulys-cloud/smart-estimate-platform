using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// The column precision of every price, norm, quantity and percentage (BR-017, shared-technical-rules.md). The tests
/// run on SQLite, which does not enforce DECIMAL scale, so the configured model is checked instead; the MySQL
/// migration is generated from this model.
/// </summary>
public sealed class NumberPrecisionModelTests
{
    public static TheoryData<Type, string, int, int> Columns => new()
    {
        { typeof(Material), nameof(Material.Price), 18, 4 },
        { typeof(Work), nameof(Work.LabourRate), 18, 4 },
        { typeof(EstimateWorkLine), nameof(EstimateWorkLine.LabourRate), 18, 4 },
        { typeof(EstimateMaterialLine), nameof(EstimateMaterialLine.Price), 18, 4 },
        { typeof(Work), nameof(Work.LabourNorm), 18, 6 },
        { typeof(WorkMaterialLine), nameof(WorkMaterialLine.ConsumptionNorm), 18, 6 },
        { typeof(EstimateWorkLine), nameof(EstimateWorkLine.LabourNorm), 18, 6 },
        { typeof(EstimateMaterialLine), nameof(EstimateMaterialLine.ConsumptionNorm), 18, 6 },
        { typeof(EstimateWorkLine), nameof(EstimateWorkLine.Quantity), 18, 3 },
        { typeof(WorkMaterialLine), nameof(WorkMaterialLine.WastePercent), 5, 2 },
        { typeof(EstimateMaterialLine), nameof(EstimateMaterialLine.WastePercent), 5, 2 },
        { typeof(Estimate), nameof(Estimate.OverheadPercent), 5, 2 },
        { typeof(Estimate), nameof(Estimate.ProfitPercent), 5, 2 },
        { typeof(Estimate), nameof(Estimate.VatPercent), 5, 2 },
    };

    [Theory]
    [MemberData(nameof(Columns))]
    public void Column_HasTheConfiguredPrecisionAndScale(Type entity, string property, int precision, int scale)
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options);

        var column = db.Model.FindEntityType(entity)!.FindProperty(property)!;

        Assert.Equal(precision, column.GetPrecision());
        Assert.Equal(scale, column.GetScale());
    }
}
