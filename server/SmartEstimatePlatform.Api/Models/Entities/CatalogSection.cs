namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A section template in a company's catalog (FR-030), e.g. "A++ siena": a named set of catalog works, each with a
/// coefficient, that is added to an estimate with one section quantity (FR-031).
/// </summary>
public class CatalogSection : ICompanyScoped
{
    // Used by EF Core when loading a section.
    private CatalogSection()
    {
    }

    /// <summary>
    /// Creates a section with already validated values (see CatalogSectionRules) and its lines.
    /// </summary>
    public CatalogSection(string name, string unit, IEnumerable<CatalogSectionLine> lines)
    {
        Update(name, unit);
        ReplaceLines(lines);
    }

    public int Id { get; private set; }

    /// <summary>
    /// Trimmed name. Together with <see cref="Unit"/> it is unique in the company, compared case-insensitively.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The section's unit, one of <see cref="MeasurementUnit.All"/>; the section quantity is entered in it.
    /// </summary>
    public string Unit { get; private set; } = string.Empty;

    public int CompanyId { get; private set; }

    public Company Company { get; private set; } = null!;

    /// <summary>
    /// The section's works in order. The same work may appear on several lines.
    /// </summary>
    public List<CatalogSectionLine> Lines { get; private set; } = [];

    /// <summary>
    /// Replaces the name and unit with already validated values.
    /// </summary>
    public void Update(string name, string unit)
    {
        Name = name;
        Unit = unit;
    }

    /// <summary>
    /// Replaces the whole list of works with the given lines.
    /// </summary>
    public void ReplaceLines(IEnumerable<CatalogSectionLine> lines)
    {
        Lines.Clear();
        Lines.AddRange(lines);
    }
}
