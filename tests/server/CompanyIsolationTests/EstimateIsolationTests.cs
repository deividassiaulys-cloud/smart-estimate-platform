using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CompanyIsolationTests;

/// <summary>
/// A Manager can never see or change another company's estimates or their lines; they are reported as not found.
/// </summary>
public sealed class EstimateIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnotherCompanysEstimate_IsNotFoundForEveryEndpoint()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var alphaWork = await alpha.CreateWallWorkAsync();
        var betaWork = await beta.CreateWallWorkAsync();
        var estimate = await alpha.CreateEstimateAsync();
        var lineId = (await alpha.Client.AddWorkLineAsync(estimate.Id, alphaWork, 35m)).WorkLines[0].Id;

        var responses = new[]
        {
            await beta.Client.GetAsync($"/api/projects/{estimate.ProjectId}/estimates"),
            await beta.Client.PostAsJsonAsync($"/api/projects/{estimate.ProjectId}/estimates", new CreateEstimateRequest { Name = "Svetima" }),
            await beta.Client.GetAsync($"/api/estimates/{estimate.Id}"),
            await beta.Client.PostWorkLineAsync(estimate.Id, betaWork, 1m),
            await beta.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/lines/{lineId}", new ChangeQuantityRequest { Quantity = 1m }),
            await beta.Client.PatchPercentagesAsync(estimate.Id, 1m, 1m, 1m),
            await beta.Client.DeleteAsync($"/api/estimates/{estimate.Id}/lines/{lineId}"),
            await beta.Client.ConfirmEstimateAsync(estimate.Id),
            await beta.Client.ReturnEstimateToDraftAsync(estimate.Id)
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        var unchanged = await alpha.Client.GetEstimateAsync(estimate.Id);
        Assert.Equal((35m, 0m), (Assert.Single(unchanged.WorkLines).Quantity, unchanged.OverheadPercent));
        Assert.Equal(SmartEstimatePlatform.Api.Models.Entities.EstimateStatus.Draft, unchanged.Status);
    }

    [Fact]
    public async Task AnotherCompanysEstimateLines_AreInvisibleToItsQueries()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var workId = await alpha.CreateWallWorkAsync();
        var estimate = await alpha.CreateEstimateAsync();
        await alpha.Client.AddWorkLineAsync(estimate.Id, workId, 35m);

        await using (var alphaDb = _factory.CreateDbContext(alpha.CompanyId))
        {
            Assert.Equal(1, await alphaDb.EstimateWorkLines.CountAsync());
            Assert.Equal(3, await alphaDb.EstimateMaterialLines.CountAsync());
        }

        await using var betaDb = _factory.CreateDbContext(beta.CompanyId);
        Assert.Empty(await betaDb.Estimates.ToListAsync());
        Assert.Empty(await betaDb.EstimateWorkLines.ToListAsync());
        Assert.Empty(await betaDb.EstimateMaterialLines.ToListAsync());
    }
}
