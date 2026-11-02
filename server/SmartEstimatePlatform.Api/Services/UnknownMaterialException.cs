namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when a work line refers to a material that is not in the current company's catalog.
/// Another company's material is reported exactly like one that does not exist.
/// </summary>
public class UnknownMaterialException(int materialId)
    : Exception($"Material {materialId} is not in the catalog.");
