namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// A catalog work's current values and composition, in its lines' order, as a catalog refresh reads them.
/// </summary>
public sealed record CatalogWork(string Name, string Unit, decimal LabourRate, decimal LabourNorm, IReadOnlyList<CatalogComponent> Components);
