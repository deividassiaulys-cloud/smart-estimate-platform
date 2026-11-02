using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CompanyIsolationTests;

/// <summary>
/// A user can never see or change another company's projects; they are reported as not found.
/// </summary>
public sealed class ProjectIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnotherCompanysProject_IsNotListedAndCannotBeReadChangedOrDeleted()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var alphaProject = await alpha.Client.CreateProjectAsync("Buto remontas");

        var betaList = await beta.Client.ListProjectsAsync();
        var get = await beta.Client.GetAsync($"/api/projects/{alphaProject.Id}");
        var update = await beta.Client.PutAsJsonAsync($"/api/projects/{alphaProject.Id}", new ProjectRequest { Name = "Pakeista" });
        var delete = await beta.Client.DeleteAsync($"/api/projects/{alphaProject.Id}");

        Assert.Empty(betaList);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("Buto remontas", Assert.Single(await alpha.Client.ListProjectsAsync()).Name);
    }
}
