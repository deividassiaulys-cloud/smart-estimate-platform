using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CompanyIsolationTests;

/// <summary>
/// A Manager can never see or act on another company's Viewers; they are reported as not found.
/// </summary>
public sealed class TeamIsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task AnotherCompanysViewer_IsNotListedAndCannotBeRemovedOrReset()
    {
        var alpha = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var beta = await _factory.RegisterCompanyAsync("Beta UAB", "beta@example.com");
        var created = await alpha.Client.PostAsJsonAsync("/api/team/viewers", new CreateViewerRequest
        {
            FirstName = "Ona",
            LastName = "Onaitė",
            Email = "ona@example.com",
            Password = "Viewer123"
        });
        var alphaViewer = (await created.Content.ReadFromJsonAsync<TeamMemberResponse>())!;

        var betaTeam = await beta.Client.GetFromJsonAsync<List<TeamMemberResponse>>("/api/team");
        var remove = await beta.Client.DeleteAsync($"/api/team/viewers/{alphaViewer.Id}");
        var reset = await beta.Client.PatchAsJsonAsync($"/api/team/viewers/{alphaViewer.Id}/password",
            new ResetPasswordRequest { NewPassword = "Hijacked123" });

        Assert.DoesNotContain(betaTeam!, member => member.Id == alphaViewer.Id);
        Assert.Equal(HttpStatusCode.NotFound, remove.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reset.StatusCode);

        // The response is the same as for an ID that does not exist at all.
        var unknown = await beta.Client.DeleteAsync("/api/team/viewers/999999");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        var unknownProblem = await unknown.Content.ReadFromJsonAsync<ProblemDetails>();
        var removeProblem = await remove.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(unknownProblem!.Title, removeProblem!.Title);
        Assert.Equal(unknownProblem.Detail, removeProblem.Detail);

        // Alpha's Viewer is untouched.
        Assert.Equal(HttpStatusCode.OK, (await _factory.LoginAsync("ona@example.com", "Viewer123")).StatusCode);
        Assert.Equal(2, (await alpha.Client.GetFromJsonAsync<List<TeamMemberResponse>>("/api/team"))!.Count);
    }
}
