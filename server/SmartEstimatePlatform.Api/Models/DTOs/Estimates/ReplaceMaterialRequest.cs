namespace SmartEstimatePlatform.Api.Models.DTOs.Estimates;

/// <summary>
/// The catalog material that replaces an estimate material line's material.
/// </summary>
public sealed record ReplaceMaterialRequest
{
    /// <summary>
    /// A material of the current company's catalog with the same unit as the line.
    /// </summary>
    public int MaterialId { get; init; }
}
