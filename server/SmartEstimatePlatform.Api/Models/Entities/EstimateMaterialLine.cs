namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A material generated for an estimate work line from the work's composition, with a copy of the material's
/// name, unit and price and of the line's norm and waste (BR-010). Its quantity is calculated, not stored.
/// </summary>
public class EstimateMaterialLine
{
    // Used by EF Core when loading a line.
    private EstimateMaterialLine()
    {
    }

    /// <summary>
    /// Creates a material line with copied values.
    /// </summary>
    public EstimateMaterialLine(
        string materialName, string materialUnit, decimal price, decimal consumptionNorm, decimal wastePercent)
    {
        MaterialName = materialName;
        MaterialUnit = materialUnit;
        Price = price;
        ConsumptionNorm = consumptionNorm;
        WastePercent = wastePercent;
    }

    public int Id { get; private set; }

    public int WorkLineId { get; private set; }

    /// <summary>
    /// The work line this material belongs to.
    /// </summary>
    public EstimateWorkLine WorkLine { get; private set; } = null!;

    public string MaterialName { get; private set; } = string.Empty;

    public string MaterialUnit { get; private set; } = string.Empty;

    /// <summary>
    /// Price per unit of the material in euros, as copied from the catalog.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Material quantity per unit of work, as copied from the work's composition.
    /// </summary>
    public decimal ConsumptionNorm { get; private set; }

    /// <summary>
    /// Waste percentage from 0 to 100, as copied from the work's composition.
    /// </summary>
    public decimal WastePercent { get; private set; }

    /// <summary>
    /// Replaces the price with an already validated value; the catalog material is not changed.
    /// </summary>
    public void ChangePrice(decimal price) => Price = price;

    /// <summary>
    /// Takes another material's name, unit and price, keeping this line's norm and waste.
    /// </summary>
    public void ReplaceMaterial(string materialName, string materialUnit, decimal price)
    {
        MaterialName = materialName;
        MaterialUnit = materialUnit;
        Price = price;
    }
}
