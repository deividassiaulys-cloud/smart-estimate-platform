namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The limits for a project's fields, defined once and used by the request and the database model.
/// </summary>
public static class ProjectRules
{
    /// <summary>
    /// Longest project name accepted, after trimming.
    /// </summary>
    public const int NameMaxLength = 200;

    /// <summary>
    /// Longest project description accepted.
    /// </summary>
    public const int DescriptionMaxLength = 2000;
}
