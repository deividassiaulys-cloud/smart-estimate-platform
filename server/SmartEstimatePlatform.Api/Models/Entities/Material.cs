namespace SmartEstimatePlatform.Api.Models.Entities;

/// <summary>
/// A material in a company's catalog, reused across that company's works.
/// </summary>
public class Material : ICompanyScoped
{
    // Used by EF Core when loading a material.
    private Material()
    {
    }

    /// <summary>
    /// Creates a material with already validated values (see MaterialRules).
    /// </summary>
    public Material(string name, string unit, decimal price)
    {
        Update(name, unit, price);
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
    /// Price per unit in euros: zero or more, with at most 2 decimal places.
    /// </summary>
    public decimal Price { get; private set; }

    public int CompanyId { get; private set; }

    public Company Company { get; private set; } = null!;

    /// <summary>
    /// Replaces the name, unit and price with already validated values.
    /// </summary>
    public void Update(string name, string unit, decimal price)
    {
        Name = name;
        Unit = unit;
        Price = price;
    }
}
