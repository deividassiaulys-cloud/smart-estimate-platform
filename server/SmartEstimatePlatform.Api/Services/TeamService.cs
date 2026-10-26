using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Lets a Manager manage the Viewer accounts of their own company. The company scoping from feature 011
/// means another company's accounts are never visible here and are reported as not found.
/// </summary>
public class TeamService(
    AppDbContext db,
    AuthService authService,
    IPasswordHasher<User> passwordHasher,
    ICurrentCompany currentCompany)
{
    /// <summary>
    /// Returns everyone in the current company, the Manager first, then Viewers by name.
    /// </summary>
    public Task<List<User>> ListMembersAsync(CancellationToken cancellationToken = default) =>
        db.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Creates a Viewer account in the current company.
    /// </summary>
    /// <exception cref="WeakPasswordException">The password breaks the <see cref="PasswordRules"/>.</exception>
    /// <exception cref="DuplicateEmailException">The email is used by any account in any company.</exception>
    public async Task<User> CreateViewerAsync(CreateViewerRequest request, CancellationToken cancellationToken = default)
    {
        var viewer = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email.Trim(),
            Role = UserRole.Viewer,
            CompanyId = currentCompany.CompanyId
                ?? throw new InvalidOperationException("A Viewer can only be created by a logged-in Manager.")
        };

        await authService.CreateAccountAsync(viewer, request.Password, cancellationToken);
        return viewer;
    }

    /// <summary>
    /// Deletes a Viewer account. Its open sessions stop working at once, because every request checks
    /// that the user still exists. The email becomes free to use again.
    /// </summary>
    /// <exception cref="NotFoundException">
    /// No Viewer with this ID in the current company. The Manager is not a Viewer, so cannot be removed.
    /// </exception>
    public async Task RemoveViewerAsync(int viewerId, CancellationToken cancellationToken = default)
    {
        var viewer = await FindViewerAsync(viewerId, cancellationToken);
        db.Users.Remove(viewer);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sets a new password for a Viewer and logs out all their open sessions. Any login lockout is lifted,
    /// so the Viewer can log in with the new password straight away.
    /// </summary>
    /// <exception cref="NotFoundException">No Viewer with this ID in the current company.</exception>
    /// <exception cref="WeakPasswordException">The password breaks the <see cref="PasswordRules"/>.</exception>
    public async Task ResetPasswordAsync(int viewerId, string newPassword, CancellationToken cancellationToken = default)
    {
        var viewer = await FindViewerAsync(viewerId, cancellationToken);
        PasswordRules.EnsureStrong(newPassword, viewer.Email);

        viewer.PasswordHash = passwordHasher.HashPassword(viewer, newPassword);
        viewer.TokenVersion++;
        viewer.FailedLoginCount = 0;
        viewer.FirstFailedLoginAt = null;
        viewer.LockoutEnd = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> FindViewerAsync(int viewerId, CancellationToken cancellationToken) =>
        await db.Users.SingleOrDefaultAsync(u => u.Id == viewerId && u.Role == UserRole.Viewer, cancellationToken)
        ?? throw new NotFoundException("Viewer not found.");
}
