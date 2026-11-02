namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when deleting a material that is used in the composition of one or more works.
/// </summary>
public class MaterialInUseException(IReadOnlyList<string> workNames)
    : Exception("The material is used in works and cannot be deleted.")
{
    /// <summary>
    /// Names of the works that use the material, A–Z, each once.
    /// </summary>
    public IReadOnlyList<string> WorkNames { get; } = workNames;
}
