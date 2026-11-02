using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.DTOs.Materials;
using SmartEstimatePlatform.Api.Models.DTOs.Team;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services.Mapping;

/// <summary>
/// The one place where entities are turned into response DTOs, so DTOs never depend on entities.
/// </summary>
public static class DtoMapper
{
    /// <summary>
    /// A material as shown in the Manager's catalog.
    /// </summary>
    public static MaterialResponse ToResponse(this Material material) => new()
    {
        Id = material.Id,
        Name = material.Name,
        Unit = material.Unit,
        Price = material.Price
    };

    /// <summary>
    /// A member of the Manager's company as shown on the Team page.
    /// </summary>
    public static TeamMemberResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role
    };

    /// <summary>
    /// The logged-in user as shown on the Dashboard. The user's company must be loaded.
    /// </summary>
    public static CurrentUserResponse ToCurrentUserResponse(this User user) => new()
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        CompanyName = user.Company.Name,
        Role = user.Role
    };

    /// <summary>
    /// The result of a registration or login, carrying the token issued for the user.
    /// </summary>
    public static AuthResponse ToAuthResponse(this User user, string token) => new()
    {
        Token = token,
        UserId = user.Id,
        CompanyId = user.CompanyId,
        Role = user.Role
    };
}
