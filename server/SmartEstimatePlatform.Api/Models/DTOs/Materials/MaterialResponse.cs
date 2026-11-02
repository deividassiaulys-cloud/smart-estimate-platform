namespace SmartEstimatePlatform.Api.Models.DTOs.Materials;

/// <summary>
/// A material as shown in the Manager's catalog.
/// </summary>
public sealed record MaterialResponse
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal Price { get; init; }
}
