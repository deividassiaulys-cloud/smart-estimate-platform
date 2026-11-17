namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a work quantity computed from a section (section quantity × coefficient) rounds to 0 (BR-007, BR-017).
/// </summary>
public class ZeroSectionQuantityException(string workName)
    : Exception($"The quantity of '{workName}' rounds to 0; enter a larger section quantity.");
