namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// One material of a catalog work's composition, with its current norm and waste.
/// </summary>
public sealed record CatalogComponent(string MaterialName, string MaterialUnit, decimal ConsumptionNorm, decimal WastePercent);
