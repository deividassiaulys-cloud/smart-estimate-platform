using SmartEstimatePlatform.Api.Models.DTOs.Team;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Lets a Manager manage the Viewer accounts of their own company. The company scoping from feature 011
/// means another company's accounts are never visible here and are reported as not found.
/// </summary>
public interface ITeamService
{
    /// <summary>
    /// Returns everyone in the current company, the Manager first, then Viewers by name.
    /// </summary>
    Task<List<TeamMemberResponse>> ListMembersAsync();

    /// <summary>
    /// Creates a Viewer account in the current company. The request has already checked the password.
    /// </summary>
    /// <exception cref="DuplicateEmailException">The email is used by any account in any company.</exception>
    Task<TeamMemberResponse> CreateViewerAsync(CreateViewerRequest request);

    /// <summary>
    /// Deletes a Viewer account. Its open sessions stop working at once, because every request checks
    /// that the user still exists. The email becomes free to use again.
    /// </summary>
    /// <exception cref="NotFoundException">
    /// No Viewer with this ID in the current company. The Manager is not a Viewer, so cannot be removed.
    /// </exception>
    Task RemoveViewerAsync(int viewerId);

    /// <summary>
    /// Sets a new password for a Viewer and logs out all their open sessions. Any login lockout is lifted,
    /// so the Viewer can log in with the new password straight away.
    /// </summary>
    /// <exception cref="NotFoundException">No Viewer with this ID in the current company.</exception>
    /// <exception cref="WeakPasswordException">The password equals the Viewer's email (the request has already checked its length).</exception>
    Task ResetPasswordAsync(int viewerId, string newPassword);
}
