namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The company of the user making the current request. It comes only from the user's validated token,
/// never from data the client sends.
/// </summary>
public interface ICurrentCompany
{
    /// <summary>
    /// The current company's ID, or null when nobody is logged in (then no company data is visible).
    /// </summary>
    int? CompanyId { get; }
}
