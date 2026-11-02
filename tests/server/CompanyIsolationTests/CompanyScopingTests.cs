using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartEstimatePlatform.Api.Controllers;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Models.DTOs.Auth;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Services;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CompanyIsolationTests;

/// <summary>
/// Integration tests that no user can see or act on another company's data.
/// </summary>
public sealed class CompanyScopingTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Me_ReturnsOnlyTheCallersOwnCompany()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        var alphaMe = await alpha.Client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        var betaMe = await beta.Client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        Assert.Equal("Alpha UAB", alphaMe!.CompanyName);
        Assert.Equal("Beta UAB", betaMe!.CompanyName);
    }

    [Fact]
    public async Task Queries_NeverSeeAnotherCompanysRecords()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        await using var db = CreateDbContext(alpha.CompanyId);

        var visible = await db.Users.Select(u => u.Id).ToListAsync();
        Assert.Equal([alpha.ManagerId], visible);
        Assert.Null(await db.Users.SingleOrDefaultAsync(u => u.Id == beta.ManagerId));
    }

    [Fact]
    public async Task Queries_WithoutALoggedInUser_SeeNothing()
    {
        await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        await using var db = CreateDbContext(companyId: null);

        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task NewRecords_AreAssignedToTheCurrentCompany_WhateverCompanyIsSet()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        await using (var db = CreateDbContext(alpha.CompanyId))
        {
            var viewer = new User("Ona", "Onaitė", "ona@example.com", UserRole.Viewer, beta.CompanyId);
            viewer.SetPasswordHash("not-used");
            db.Users.Add(viewer);
            await db.SaveChangesAsync();
        }

        await using var check = CreateDbContext(companyId: null);
        var saved = await check.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == "ona@example.com");
        Assert.Equal(alpha.CompanyId, saved.CompanyId);
    }

    [Fact]
    public async Task Token_WhoseCompanyDoesNotMatchTheUser_IsRejected()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");

        // A correctly signed token for Alpha's Manager that claims Beta's company.
        string forgedToken;
        using (var scope = _factory.Services.CreateScope())
        {
            // The real user, with only its company changed in memory (never saved).
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var manager = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == alpha.ManagerId);
            db.Entry(manager).Property(u => u.CompanyId).CurrentValue = beta.CompanyId;
            forgedToken = scope.ServiceProvider.GetRequiredService<ITokenService>().CreateToken(manager);
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forgedToken);
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotFoundException_IsReturnedAs404()
    {
        using var scope = _factory.Services.CreateScope();
        var handler = ActivatorUtilities.CreateInstance<DomainExceptionHandler>(scope.ServiceProvider);
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, new NotFoundException(), CancellationToken.None);
        var ignored = await handler.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException(), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(ignored);
    }

    [Fact]
    public void OnlyAuthService_BypassesTheCompanyScoping()
    {
        var apiDirectory = Path.Combine(FindRepositoryRoot(), "server", "SmartEstimatePlatform.Api");

        var bypassingFiles = Directory
            .EnumerateFiles(apiDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains(".IgnoreQueryFilters("))
            .Select(Path.GetFileName)
            .ToList();

        Assert.Equal(["AuthService.cs"], bypassingFiles);
    }

    private AppDbContext CreateDbContext(int? companyId)
    {
        _factory.CreateClient(); // Makes sure the database exists.
        var options = _factory.Services.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>>();
        return new AppDbContext(options, new FixedCurrentUser(companyId));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "server", "SmartEstimatePlatform.Api")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
