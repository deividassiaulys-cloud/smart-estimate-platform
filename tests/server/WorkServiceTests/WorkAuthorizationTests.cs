using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.WorkServiceTests;

/// <summary>
/// Works contain labour prices, so Viewers can never read or change the work catalog.
/// </summary>
public sealed class WorkAuthorizationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Viewer_IsForbiddenFromEveryWorkEndpoint()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var work = await company.CreateWorkAsync(WorkRequest("Sienų dažymas"));
        var viewer = await _factory.CreateViewerClientAsync(company);
        var request = WorkRequest("Lubų dažymas");

        var responses = new[]
        {
            await viewer.GetAsync("/api/works"),
            await viewer.GetAsync($"/api/works/{work.Id}"),
            await viewer.PostAsJsonAsync("/api/works", request),
            await viewer.PutAsJsonAsync($"/api/works/{work.Id}", request),
            await viewer.DeleteAsync($"/api/works/{work.Id}")
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        Assert.Equal("Sienų dažymas", Assert.Single(await company.ListWorksAsync()).Name);
    }

    [Fact]
    public async Task AnonymousRequest_IsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/works");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
