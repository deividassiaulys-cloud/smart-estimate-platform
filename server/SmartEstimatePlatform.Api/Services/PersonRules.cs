namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The length limits of an account's personal data and its company's name (feature 021, F9), defined once and used
/// by the requests and the database model, so over-long input is a 400, never a database error.
/// </summary>
public static class PersonRules
{
    /// <summary>
    /// Longest first name or last name accepted.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Longest email accepted; the size of the Users.Email column.
    /// </summary>
    public const int EmailMaxLength = 256;

    /// <summary>
    /// Longest company name accepted.
    /// </summary>
    public const int CompanyNameMaxLength = 200;
}
