namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A work in a company's catalog: its labour and the materials it consumes per unit of work.
/// Adding it to an estimate generates its material lines (feature 007).
/// </summary>
public class Work : ICompanyScoped
{
    // Used by EF Core when loading a work.
    private Work()
    {
    }

    /// <summary>
    /// Creates a work with already validated values (see WorkRules) and no material lines.
    /// </summary>
    public Work(string name, string unit, decimal labourRate, decimal labourNorm)
    {
        Update(name, unit, labourRate, labourNorm);
    }

    public int Id { get; private set; }

    /// <summary>
    /// Trimmed name. Together with <see cref="Unit"/> it is unique in the company, compared case-insensitively.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// One of <see cref="MeasurementUnit.All"/>.
    /// </summary>
    public string Unit { get; private set; } = string.Empty;

    /// <summary>
    /// Labour rate in euros per hour.
    /// </summary>
    public decimal LabourRate { get; private set; }

    /// <summary>
    /// Labour hours per unit of work; 0 when the work needs no labour.
    /// </summary>
    public decimal LabourNorm { get; private set; }

    public int CompanyId { get; private set; }

    public Company Company { get; private set; } = null!;

    /// <summary>
    /// The materials this work consumes. The same material may appear on several lines (BR-009).
    /// </summary>
    public List<WorkMaterialLine> Lines { get; private set; } = [];

    /// <summary>
    /// Replaces the name, unit, labour rate and labour norm with already validated values.
    /// </summary>
    public void Update(string name, string unit, decimal labourRate, decimal labourNorm)
    {
        Name = name;
        Unit = unit;
        LabourRate = labourRate;
        LabourNorm = labourNorm;
    }

    /// <summary>
    /// Replaces the whole composition with the given lines.
    /// </summary>
    public void ReplaceLines(IEnumerable<WorkMaterialLine> lines)
    {
        Lines.Clear();
        Lines.AddRange(lines);
    }
}
