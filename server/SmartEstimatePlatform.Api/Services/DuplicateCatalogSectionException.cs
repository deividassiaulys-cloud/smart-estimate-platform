namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when the company already has a catalog section with the same name (ignoring letter case) and unit.
/// </summary>
public class DuplicateCatalogSectionException(string name, string unit)
    : Exception($"A catalog section '{name}' with unit '{unit}' already exists.");
