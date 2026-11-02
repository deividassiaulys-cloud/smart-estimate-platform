using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.ProjectServiceTests;

/// <summary>
/// Viewers may read their company's projects but never create, change or delete them.
/// </summary>
public sealed class ProjectAuthorizationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Viewer_CanListAndOpenProjects()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var project = await company.Client.CreateProjectAsync("Buto remontas", "Vilnius");
        var viewer = await _factory.CreateViewerClientAsync(company);

        var listed = Assert.Single(await viewer.ListProjectsAsync());
        var detail = await viewer.GetFromJsonAsync<ProjectResponse>($"/api/projects/{project.Id}");

        Assert.Equal(("Buto remontas", "Vilnius"), (listed.Name, listed.Description));
        Assert.Equal(project.Id, detail!.Id);
    }

    [Fact]
    public async Task Viewer_IsForbiddenToCreateChangeOrDelete()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var project = await company.Client.CreateProjectAsync("Buto remontas");
        var viewer = await _factory.CreateViewerClientAsync(company);
        var request = new ProjectRequest { Name = "Pakeista" };

        var responses = new[]
        {
            await viewer.PostAsJsonAsync("/api/projects", request),
            await viewer.PutAsJsonAsync($"/api/projects/{project.Id}", request),
            await viewer.DeleteAsync($"/api/projects/{project.Id}")
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        Assert.Equal("Buto remontas", Assert.Single(await company.Client.ListProjectsAsync()).Name);
    }

    [Fact]
    public async Task AnonymousRequest_IsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
