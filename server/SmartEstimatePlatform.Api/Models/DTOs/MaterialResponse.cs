using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Models.DTOs;

/// <summary>
/// A material as shown in the Manager's catalog.
/// </summary>
public class MaterialResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public static MaterialResponse From(Material material) => new()
    {
        Id = material.Id,
        Name = material.Name,
        Unit = material.Unit,
        Price = material.Price
    };
}
