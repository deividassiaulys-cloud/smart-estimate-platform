namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// A line whose work (no material name) or material is no longer in the catalog; it stays as it is.
/// </summary>
public sealed record RefreshMiss(string WorkName, string? MaterialName);
