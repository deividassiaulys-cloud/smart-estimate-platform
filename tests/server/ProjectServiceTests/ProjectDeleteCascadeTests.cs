using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.ProjectServiceTests;

/// <summary>
/// Deleting a project deletes its estimates and all their lines, and nothing else (BR-011).
/// </summary>
public sealed class ProjectDeleteCascadeTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task DeletingAProject_DeletesItsEstimatesAndTheirLinesOnly()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var deleted = await company.CreateEstimateAsync("Pašalinama");
        await company.Client.AddWorkLineAsync(deleted.Id, workId, 35m);
        var kept = await company.CreateEstimateAsync("Lieka");
        await company.Client.AddWorkLineAsync(kept.Id, workId, 10m);

        var response = await company.Client.DeleteAsync($"/api/projects/{deleted.ProjectId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await company.Client.GetAsync($"/api/estimates/{deleted.Id}")).StatusCode);
        var remaining = (await company.Client.GetFromJsonAsync<List<EstimateSummaryResponse>>($"/api/projects/{kept.ProjectId}/estimates"))!;
        Assert.Equal("Lieka", Assert.Single(remaining).Name);

        await using var db = _factory.CreateDbContext(company.CompanyId);
        Assert.Equal(1, await db.Estimates.CountAsync());
        Assert.Equal(1, await db.EstimateWorkLines.CountAsync());
        Assert.Equal(3, await db.EstimateMaterialLines.CountAsync());
    }
}
