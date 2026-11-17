namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// One work of a catalog section with its coefficient: how many units of the work one unit of the section needs
/// (e.g. 0,4 m of barrier per m² of wall). It belongs to the company of its section.
/// </summary>
public class CatalogSectionLine
{
    // Used by EF Core when loading a line.
    private CatalogSectionLine()
    {
    }

    /// <summary>
    /// Creates a line with an already validated coefficient (see CatalogSectionRules).
    /// </summary>
    public CatalogSectionLine(int workId, decimal coefficient)
    {
        WorkId = workId;
        Coefficient = coefficient;
    }

    public int Id { get; private set; }

    public int CatalogSectionId { get; private set; }

    /// <summary>
    /// The section this line belongs to; its company is the line's company.
    /// </summary>
    public CatalogSection CatalogSection { get; private set; } = null!;

    public int WorkId { get; private set; }

    public Work Work { get; private set; } = null!;

    /// <summary>
    /// Work quantity per 1 unit of the section; greater than 0, at most 6 decimal places.
    /// </summary>
    public decimal Coefficient { get; private set; }
}
