namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// A catalog material's current values, as a catalog refresh reads them.
/// </summary>
public sealed record CatalogMaterial(string Name, string Unit, decimal Price);
