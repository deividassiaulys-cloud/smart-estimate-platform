namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Thrown when deleting a work that is used in one or more catalog sections (FR-030).
/// </summary>
public class WorkInUseException(IReadOnlyList<string> sectionNames)
    : Exception("The work is used in catalog sections and cannot be deleted.")
{
    /// <summary>
    /// Names of the catalog sections that use the work, A–Z, each once.
    /// </summary>
    public IReadOnlyList<string> SectionNames { get; } = sectionNames;
}
