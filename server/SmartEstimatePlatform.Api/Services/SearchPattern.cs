namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Builds LIKE patterns for the catalogs' partial-name search.
/// </summary>
public static class SearchPattern
{
    /// <summary>
    /// The escape character used in the patterns; pass it to EF.Functions.Like.
    /// </summary>
    public const string Escape = @"\";

    /// <summary>
    /// A pattern matching names that contain <paramref name="search"/> (trimmed). The % and _ the user
    /// typed match literally.
    /// </summary>
    public static string Contains(string search) =>
        "%" + search.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
}
