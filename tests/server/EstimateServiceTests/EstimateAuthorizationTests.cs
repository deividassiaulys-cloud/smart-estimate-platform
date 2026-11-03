using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.EstimateServiceTests;

/// <summary>
/// Estimates contain prices, so until feature 009 adds the names-only view, Viewers get none of them.
/// </summary>
public sealed class EstimateAuthorizationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Viewer_IsForbiddenFromEveryEstimateEndpoint()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        var lineId = (await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m)).WorkLines[0].Id;
        var viewer = await _factory.CreateViewerClientAsync(company);

        var responses = new[]
        {
            await viewer.GetAsync($"/api/projects/{estimate.ProjectId}/estimates"),
            await viewer.PostAsJsonAsync($"/api/projects/{estimate.ProjectId}/estimates", new CreateEstimateRequest { Name = "Sąmata" }),
            await viewer.GetAsync($"/api/estimates/{estimate.Id}"),
            await viewer.PostWorkLineAsync(estimate.Id, workId, 1m),
            await viewer.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/lines/{lineId}", new ChangeQuantityRequest { Quantity = 1m }),
            await viewer.PatchPercentagesAsync(estimate.Id, 1m, 1m, 1m),
            await viewer.DeleteAsync($"/api/estimates/{estimate.Id}/lines/{lineId}"),
            await viewer.ConfirmEstimateAsync(estimate.Id),
            await viewer.ReturnEstimateToDraftAsync(estimate.Id)
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        Assert.Single((await company.Client.GetEstimateAsync(estimate.Id)).WorkLines);
    }

    [Fact]
    public async Task AnonymousRequest_IsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/estimates/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
