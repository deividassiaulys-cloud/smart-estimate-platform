namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when the company already has a work with the same name (ignoring letter case) and unit.
/// </summary>
public class DuplicateWorkException(string name, string unit)
    : Exception($"A work '{name}' with unit '{unit}' already exists.");
