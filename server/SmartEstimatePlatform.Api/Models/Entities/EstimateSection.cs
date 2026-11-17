namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A section of an estimate (FR-031), with a copy of its catalog section's name and unit and the section quantity.
/// It does not refer to the catalog (BR-010), so later catalog changes or deletions never affect it.
/// </summary>
public class EstimateSection
{
    // Used by EF Core when loading a section.
    private EstimateSection()
    {
    }

    /// <summary>
    /// Creates a section with copied, already validated values.
    /// </summary>
    public EstimateSection(string name, string unit, decimal quantity)
    {
        Name = name;
        Unit = unit;
        Quantity = quantity;
    }

    public int Id { get; private set; }

    public int EstimateId { get; private set; }

    /// <summary>
    /// The estimate this section belongs to; its company is the section's company.
    /// </summary>
    public Estimate Estimate { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    /// <summary>
    /// How many units of the section the estimate contains; greater than 0, at most 3 decimal places.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>
    /// Replaces the section quantity with an already validated value; the service recalculates the work lines.
    /// </summary>
    public void ChangeQuantity(decimal quantity) => Quantity = quantity;
}
