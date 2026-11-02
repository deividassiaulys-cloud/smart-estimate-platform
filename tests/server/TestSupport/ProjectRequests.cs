using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Projects;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Shortcuts for working with projects through the real API in tests.
/// </summary>
public static class ProjectRequests
{
    /// <summary>
    /// Creates a project and returns it; fails the test if the API rejects it.
    /// </summary>
    public static async Task<ProjectResponse> CreateProjectAsync(this HttpClient client, string name, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new ProjectRequest { Name = name, Description = description });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    /// <summary>
    /// Lists the projects the client's user can see.
    /// </summary>
    public static async Task<List<ProjectResponse>> ListProjectsAsync(this HttpClient client) =>
        (await client.GetFromJsonAsync<List<ProjectResponse>>("/api/projects"))!;
}
