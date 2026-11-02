using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Issues JWTs that identify a user, their company, and their role.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed token for the given user.
    /// </summary>
    string CreateToken(User user);
}
