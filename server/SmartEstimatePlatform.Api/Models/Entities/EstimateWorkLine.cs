namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A work added to an estimate, with a copy of the work's name, unit, labour rate and labour norm taken when it
/// was added (BR-010). It does not refer to the catalog, so later catalog changes never affect it.
/// </summary>
public class EstimateWorkLine
{
    // Used by EF Core when loading a line.
    private EstimateWorkLine()
    {
    }

    /// <summary>
    /// Creates a work line with copied, already validated values and its generated material lines.
    /// </summary>
    public EstimateWorkLine(
        string workName, string workUnit, decimal labourRate, decimal labourNorm, decimal quantity,
        IEnumerable<EstimateMaterialLine> materialLines)
    {
        WorkName = workName;
        WorkUnit = workUnit;
        LabourRate = labourRate;
        LabourNorm = labourNorm;
        Quantity = quantity;
        MaterialLines = materialLines.ToList();
    }

    public int Id { get; private set; }

    public int EstimateId { get; private set; }

    /// <summary>
    /// The estimate this line belongs to; its company is the line's company.
    /// </summary>
    public Estimate Estimate { get; private set; } = null!;

    public string WorkName { get; private set; } = string.Empty;

    public string WorkUnit { get; private set; } = string.Empty;

    /// <summary>
    /// Labour rate in euros per hour, as copied from the work.
    /// </summary>
    public decimal LabourRate { get; private set; }

    /// <summary>
    /// Labour hours per unit of work, as copied from the work.
    /// </summary>
    public decimal LabourNorm { get; private set; }

    /// <summary>
    /// How many units of the work the estimate contains; greater than 0.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// The work's materials, copied from its composition when it was added.
    /// </summary>
    public List<EstimateMaterialLine> MaterialLines { get; private set; } = [];

    /// <summary>
    /// Replaces the quantity with an already validated value.
    /// </summary>
    public void ChangeQuantity(decimal quantity) => Quantity = quantity;
}
