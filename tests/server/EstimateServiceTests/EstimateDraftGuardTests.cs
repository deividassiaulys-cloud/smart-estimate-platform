using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.EstimateServiceTests;

/// <summary>
/// A confirmed estimate cannot be changed by any means (BR-013). Feature 008 adds the way to confirm one;
/// here the status is set directly in the database to prove that every change is checked.
/// </summary>
public sealed class EstimateDraftGuardTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ConfirmedEstimate_RejectsEveryChange()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        var lineId = (await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m)).WorkLines[0].Id;
        await ConfirmInDatabaseAsync(company.CompanyId, estimate.Id);

        var responses = new[]
        {
            await company.Client.PostWorkLineAsync(estimate.Id, workId, 1m),
            await company.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/lines/{lineId}", new ChangeQuantityRequest { Quantity = 40m }),
            await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m),
            await company.Client.DeleteAsync($"/api/estimates/{estimate.Id}/lines/{lineId}")
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Conflict, response.StatusCode));
        var unchanged = await company.Client.GetEstimateAsync(estimate.Id);
        Assert.Equal((35m, 0m), (Assert.Single(unchanged.WorkLines).Quantity, unchanged.OverheadPercent));
    }

    private async Task ConfirmInDatabaseAsync(int companyId, int estimateId)
    {
        await using var db = _factory.CreateDbContext(companyId);
        var estimate = await db.Estimates.SingleAsync(e => e.Id == estimateId);
        db.Entry(estimate).Property(e => e.Status).CurrentValue = EstimateStatus.Confirmed;
        await db.SaveChangesAsync();
    }
}
