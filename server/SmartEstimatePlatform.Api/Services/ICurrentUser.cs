namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// The user making the current request and their company. Both come only from the user's validated token,
/// never from data the client sends.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The current user's ID, or null when nobody is logged in.
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// The current company's ID, or null when nobody is logged in (then no company data is visible).
    /// </summary>
    int? CompanyId { get; }
}
