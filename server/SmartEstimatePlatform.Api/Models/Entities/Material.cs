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
    public Material(string name, string unit, MaterialKind kind, decimal price)
    {
        Update(name, unit, kind, price);
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
    /// Price per unit in euros: zero or more, with at most 4 decimal places.
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// Material or Machinery (FR-030); Material unless the Manager chose otherwise.
    /// </summary>
    public MaterialKind Kind { get; private set; }

    public int CompanyId { get; private set; }

    public Company Company { get; private set; } = null!;

    /// <summary>
    /// Replaces the name, unit, kind and price with already validated values.
    /// </summary>
    public void Update(string name, string unit, MaterialKind kind, decimal price)
    {
        Name = name;
        Unit = unit;
        Kind = kind;
        Price = price;
    }
}
