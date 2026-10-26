namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A material in a company's catalog, reused across that company's works.
/// </summary>
public class Material : ICompanyScoped
{
    public const int NameMaxLength = 200;

    public int Id { get; set; }

    /// <summary>
    /// Trimmed name. Together with <see cref="Unit"/> it is unique in the company, compared case-insensitively.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// One of <see cref="MeasurementUnit.All"/>.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Price per unit in euros: zero or more, with at most 2 decimal places.
    /// </summary>
    public decimal Price { get; set; }

    public int CompanyId { get; set; }

    public Company Company { get; set; } = null!;
}
