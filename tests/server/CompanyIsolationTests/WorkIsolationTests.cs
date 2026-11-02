using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.CompanyIsolationTests;

/// <summary>
/// A Manager can never see or change another company's works; they are reported as not found.
/// </summary>
public sealed class WorkIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnotherCompanysWork_IsNotListedAndCannotBeReadChangedOrDeleted()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var alphaWork = await alpha.CreateWorkAsync(WorkRequest("Sienų dažymas"));

        var betaList = await beta.ListWorksAsync("Sien");
        var get = await beta.Client.GetAsync($"/api/works/{alphaWork.Id}");
        var update = await beta.Client.PutAsJsonAsync($"/api/works/{alphaWork.Id}", WorkRequest("Pakeista"));
        var delete = await beta.Client.DeleteAsync($"/api/works/{alphaWork.Id}");

        Assert.Empty(betaList);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("Sienų dažymas", Assert.Single(await alpha.ListWorksAsync()).Name);
    }

    [Fact]
    public async Task SameNameAndUnit_IsAllowedInDifferentCompanies()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        await alpha.CreateWorkAsync(WorkRequest("Sienų dažymas"));

        var response = await beta.PostWorkAsync(WorkRequest("Sienų dažymas"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
