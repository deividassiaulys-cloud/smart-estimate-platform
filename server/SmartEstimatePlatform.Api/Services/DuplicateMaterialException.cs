namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when the company already has a material with the same name (ignoring letter case) and unit.
/// </summary>
public class DuplicateMaterialException(string name, string unit)
    : Exception($"A material '{name}' with unit '{unit}' already exists.");
