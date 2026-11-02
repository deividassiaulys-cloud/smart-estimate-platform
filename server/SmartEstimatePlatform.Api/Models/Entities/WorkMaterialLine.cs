namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// One material in a work's composition. It has its own ID, so the same material can appear on several
/// lines of one work, each with its own norm and waste (BR-009). It belongs to the company of its work.
/// </summary>
public class WorkMaterialLine
{
    // Used by EF Core when loading a line.
    private WorkMaterialLine()
    {
    }

    /// <summary>
    /// Creates a line with already validated values (see WorkRules).
    /// </summary>
    public WorkMaterialLine(int materialId, decimal consumptionNorm, decimal wastePercent)
    {
        MaterialId = materialId;
        ConsumptionNorm = consumptionNorm;
        WastePercent = wastePercent;
    }

    public int Id { get; private set; }

    public int WorkId { get; private set; }

    /// <summary>
    /// The work this line belongs to; its company is the line's company.
    /// </summary>
    public Work Work { get; private set; } = null!;

    public int MaterialId { get; private set; }

    public Material Material { get; private set; } = null!;

    /// <summary>
    /// Material quantity per unit of work, in the material's own unit.
    /// </summary>
    public decimal ConsumptionNorm { get; private set; }

    /// <summary>
    /// Extra material for waste, as a percentage from 0 to 100.
    /// </summary>
    public decimal WastePercent { get; private set; }
}
