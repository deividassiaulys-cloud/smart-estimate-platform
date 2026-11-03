using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.EstimateServiceTests;

/// <summary>
/// Confirming an estimate and returning it to Draft (FR-019, FR-021), through the real API.
/// </summary>
public sealed class EstimateStatusTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private Task<LoggedInCompany> RegisterAsync() => _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

    [Fact]
    public async Task Confirm_MakesTheEstimateConfirmedWithUnchangedAmounts()
    {
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        var draft = await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);

        var response = await company.Client.ConfirmEstimateAsync(estimate.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmed = (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
        Assert.Equal(EstimateStatus.Confirmed, confirmed.Status);
        Assert.Equal(draft.Total, confirmed.Total);
        Assert.Equal(EstimateStatus.Confirmed, (await company.Client.GetEstimateAsync(estimate.Id)).Status);
    }

    [Fact]
    public async Task Confirm_EstimateWithoutWorks_Succeeds()
    {
        var company = await RegisterAsync();
        var estimate = await company.CreateEstimateAsync();

        var response = await company.Client.ConfirmEstimateAsync(estimate.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmTwice_IsRejected()
    {
        var company = await RegisterAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.ConfirmEstimateAsync(estimate.Id);

        var response = await company.Client.ConfirmEstimateAsync(estimate.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("The estimate is already confirmed.", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
    }

    [Fact]
    public async Task ReturnToDraft_MakesTheEstimateEditableAgain()
    {
        var company = await RegisterAsync();
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        var lineId = (await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m)).WorkLines[0].Id;
        await company.Client.ConfirmEstimateAsync(estimate.Id);

        var response = await company.Client.ReturnEstimateToDraftAsync(estimate.Id);

        Assert.Equal(EstimateStatus.Draft, (await response.Content.ReadFromJsonAsync<EstimateResponse>())!.Status);
        var change = await company.Client.PatchAsJsonAsync($"/api/estimates/{estimate.Id}/lines/{lineId}", new ChangeQuantityRequest { Quantity = 40m });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal(40m, Assert.Single((await company.Client.GetEstimateAsync(estimate.Id)).WorkLines).Quantity);
    }

    [Fact]
    public async Task ReturnToDraft_OfADraft_IsRejected()
    {
        var company = await RegisterAsync();
        var estimate = await company.CreateEstimateAsync();

        var response = await company.Client.ReturnEstimateToDraftAsync(estimate.Id);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("The estimate is already a draft.", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail);
    }

    [Fact]
    public async Task ProjectEstimateList_ShowsEachEstimatesStatus()
    {
        var company = await RegisterAsync();
        var project = await company.Client.CreateProjectAsync("Buto remontas");
        var draft = await CreateInProjectAsync(company, project.Id, "Juodraštinė");
        var confirmed = await CreateInProjectAsync(company, project.Id, "Patvirtinta");
        await company.Client.ConfirmEstimateAsync(confirmed.Id);

        var list = (await company.Client.GetFromJsonAsync<List<EstimateSummaryResponse>>($"/api/projects/{project.Id}/estimates"))!;

        Assert.Equal(EstimateStatus.Draft, list.Single(e => e.Id == draft.Id).Status);
        Assert.Equal(EstimateStatus.Confirmed, list.Single(e => e.Id == confirmed.Id).Status);
    }

    [Fact]
    public async Task MissingEstimate_Returns404()
    {
        var company = await RegisterAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await company.Client.ConfirmEstimateAsync(999)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await company.Client.ReturnEstimateToDraftAsync(999)).StatusCode);
    }

    private static async Task<EstimateResponse> CreateInProjectAsync(LoggedInCompany company, int projectId, string name)
    {
        var response = await company.Client.PostAsJsonAsync($"/api/projects/{projectId}/estimates", new CreateEstimateRequest { Name = name });
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }
}
