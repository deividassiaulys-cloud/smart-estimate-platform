using Microsoft.AspNetCore.Identity;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Handles account registration for visitors.
/// </summary>
public class AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher)
{
    /// <summary>
    /// Registers a visitor: creates their company and makes them its Manager.
    /// The company and the account are saved together, so either both exist or neither does.
    /// </summary>
    /// <returns>The created Manager account, with its company.</returns>
    public async Task<User> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var company = new Company { Name = request.CompanyName };

        var manager = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = UserRole.Manager,
            Company = company
        };
        manager.PasswordHash = passwordHasher.HashPassword(manager, request.Password);

        db.Users.Add(manager);
        await db.SaveChangesAsync(cancellationToken);

        return manager;
    }
}
