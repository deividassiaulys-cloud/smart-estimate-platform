using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Team;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services.Mapping;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Lets a Manager manage the Viewer accounts of their own company. The company scoping from feature 011
/// means another company's accounts are never visible here and are reported as not found.
/// </summary>
public class TeamService(
    AppDbContext db,
    IAuthService authService,
    IPasswordHasher<User> passwordHasher,
    ICurrentUser currentUser) : ITeamService
{
    /// <inheritdoc />
    public async Task<List<TeamMemberResponse>> ListMembersAsync()
    {
        var members = await db.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync();
        return members.Select(u => u.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<TeamMemberResponse> CreateViewerAsync(CreateViewerRequest request)
    {
        var companyId = currentUser.CompanyId
            ?? throw new InvalidOperationException("A Viewer can only be created by a logged-in Manager.");
        var viewer = new User(request.FirstName, request.LastName, request.Email.Trim(), UserRole.Viewer, companyId);

        await authService.CreateAccountAsync(viewer, request.Password);
        return viewer.ToResponse();
    }

    /// <inheritdoc />
    public async Task RemoveViewerAsync(int viewerId)
    {
        var viewer = await FindViewerAsync(viewerId);
        db.Users.Remove(viewer);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ResetPasswordAsync(int viewerId, string newPassword)
    {
        var viewer = await FindViewerAsync(viewerId);
        PasswordRules.EnsureNotEmail(newPassword, viewer.Email);

        viewer.SetPasswordHash(passwordHasher.HashPassword(viewer, newPassword));
        viewer.InvalidateTokens();
        viewer.ResetLoginFailures();
        await db.SaveChangesAsync();
    }

    private async Task<User> FindViewerAsync(int viewerId) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == viewerId && u.Role == UserRole.Viewer)
        ?? throw new NotFoundException("Viewer not found.");
}
