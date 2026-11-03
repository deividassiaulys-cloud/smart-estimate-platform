using Microsoft.AspNetCore.Identity;
using SmartEstimatePlatform.Api.Models.Entities;

namespace SmartEstimatePlatform.Api.Services;

/// <summary>
/// Registers the Application layer's services in one place, so Program.cs only wires up the framework.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every application service by its interface, plus the password hasher and clock they use.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<IWorkService, WorkService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IEstimateService, EstimateService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
