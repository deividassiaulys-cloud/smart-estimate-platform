using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.ProjectServiceTests;

/// <summary>
/// Integration tests for creating, listing, editing and deleting projects, through the real API.
/// </summary>
public sealed class ProjectEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Create_StoresTrimmedNameAndDescription()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await company.Client.PostAsJsonAsync("/api/projects",
            new ProjectRequest { Name = "  Buto remontas  ", Description = "  Klientas: J. Jonaitis  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = Assert.Single(await company.Client.ListProjectsAsync());
        Assert.Equal(("Buto remontas", "Klientas: J. Jonaitis"), (project.Name, project.Description));
    }

    [Fact]
    public async Task Create_WhitespaceOnlyDescription_IsStoredAsNone()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        await company.Client.CreateProjectAsync("Buto remontas", "   ");

        Assert.Null(Assert.Single(await company.Client.ListProjectsAsync()).Description);
    }

    [Fact]
    public async Task Create_SameNameTwice_IsAccepted()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await company.Client.CreateProjectAsync("Buto remontas", "Vilnius");

        var response = await company.Client.PostAsJsonAsync("/api/projects", new ProjectRequest { Name = "Buto remontas", Description = "Kaunas" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, (await company.Client.ListProjectsAsync()).Count);
    }

    [Fact]
    public async Task Create_EmptyName_IsRejectedAndSavesNothing()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await company.Client.PostAsJsonAsync("/api/projects", new ProjectRequest { Name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await company.Client.ListProjectsAsync());
    }

    [Fact]
    public async Task List_IsSortedByName()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await company.Client.CreateProjectAsync("Stogo keitimas");
        await company.Client.CreateProjectAsync("Buto remontas");
        await company.Client.CreateProjectAsync("Fasado šiltinimas");

        var names = (await company.Client.ListProjectsAsync()).Select(p => p.Name);

        Assert.Equal(["Buto remontas", "Fasado šiltinimas", "Stogo keitimas"], names);
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var project = await company.Client.CreateProjectAsync("Buto remontas", "Vilnius");

        var response = await company.Client.PutAsJsonAsync($"/api/projects/{project.Id}",
            new ProjectRequest { Name = "Namo remontas", Description = "Kaunas" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await company.Client.GetFromJsonAsync<ProjectResponse>($"/api/projects/{project.Id}"))!;
        Assert.Equal(("Namo remontas", "Kaunas"), (saved.Name, saved.Description));
    }

    [Fact]
    public async Task Delete_RemovesTheProject()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var project = await company.Client.CreateProjectAsync("Buto remontas");

        var response = await company.Client.DeleteAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await company.Client.ListProjectsAsync());
    }

    [Fact]
    public async Task MissingProject_Returns404()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var responses = new[]
        {
            await company.Client.GetAsync("/api/projects/999"),
            await company.Client.PutAsJsonAsync("/api/projects/999", new ProjectRequest { Name = "Buto remontas" }),
            await company.Client.DeleteAsync("/api/projects/999")
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }
}
