using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;
using SmartEstimatePlatform.Api.Services;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// An unexpected failure outside development answers 500 without any technical details (NFR-010).
/// </summary>
public sealed class UnexpectedErrorTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task UnexpectedFailure_Returns500WithoutTechnicalDetails()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        using var failing = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProjectService>();
            services.AddScoped<IProjectService, FailingProjectService>();
        }));
        var client = failing.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", company.Token);

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ProblemDetails>());
        Assert.DoesNotContain("secret detail", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
        Assert.DoesNotContain(nameof(FailingProjectService), body);
        Assert.DoesNotContain(" at ", body);
    }

    private sealed class FailingProjectService : IProjectService
    {
        public Task<List<ProjectResponse>> ListAsync() => throw new InvalidOperationException("secret detail");
        public Task<ProjectResponse> GetAsync(int id) => throw new NotSupportedException();
        public Task<ProjectResponse> CreateAsync(ProjectRequest request) => throw new NotSupportedException();
        public Task<ProjectResponse> UpdateAsync(int id, ProjectRequest request) => throw new NotSupportedException();
        public Task DeleteAsync(int id) => throw new NotSupportedException();
    }
}
