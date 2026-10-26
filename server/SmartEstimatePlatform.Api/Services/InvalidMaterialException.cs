namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a material breaks a validation rule (empty name, unit outside the fixed list, invalid price).
/// </summary>
public class InvalidMaterialException(string message) : Exception(message);
