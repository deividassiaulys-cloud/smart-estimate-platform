namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when an estimate material line would be replaced by a material with another unit; the line keeps its
/// consumption norm, which only makes sense in the same unit (spec 014 Q1).
/// </summary>
public class MaterialUnitMismatchException()
    : Exception("The replacement material must have the same unit.");
