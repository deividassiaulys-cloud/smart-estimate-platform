using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Every protected endpoint refuses a request without a token (feature 021, F2; docs/security-review.md section 4).
/// The list below is also compared with the API's real endpoints, so a new endpoint cannot be left out.
/// </summary>
public sealed class AnonymousAccessTests : IDisposable
{
    // Method and route template exactly as the API declares them; only register and login are anonymous.
    private static readonly (string Method, string Route)[] ProtectedRoutes =
    [
        ("GET", "api/auth/me"),
        ("POST", "api/auth/logout"),
        ("GET", "api/units"),
        ("GET", "api/materials"),
        ("GET", "api/materials/{id:int}"),
        ("POST", "api/materials"),
        ("PUT", "api/materials/{id:int}"),
        ("DELETE", "api/materials/{id:int}"),
        ("GET", "api/works"),
        ("GET", "api/works/{id:int}"),
        ("POST", "api/works"),
        ("PUT", "api/works/{id:int}"),
        ("DELETE", "api/works/{id:int}"),
        ("GET", "api/catalog-sections"),
        ("GET", "api/catalog-sections/{id:int}"),
        ("POST", "api/catalog-sections"),
        ("PUT", "api/catalog-sections/{id:int}"),
        ("DELETE", "api/catalog-sections/{id:int}"),
        ("GET", "api/projects"),
        ("GET", "api/projects/{id:int}"),
        ("POST", "api/projects"),
        ("PUT", "api/projects/{id:int}"),
        ("DELETE", "api/projects/{id:int}"),
        ("GET", "api/projects/{projectId:int}/estimates"),
        ("POST", "api/projects/{projectId:int}/estimates"),
        ("GET", "api/estimates/{id:int}"),
        ("POST", "api/estimates/{id:int}/lines"),
        ("PATCH", "api/estimates/{id:int}/lines/{lineId:int}"),
        ("DELETE", "api/estimates/{id:int}/lines/{lineId:int}"),
        ("POST", "api/estimates/{id:int}/sections"),
        ("PATCH", "api/estimates/{id:int}/sections/{sectionId:int}"),
        ("DELETE", "api/estimates/{id:int}/sections/{sectionId:int}"),
        ("PATCH", "api/estimates/{id:int}/lines/{lineId:int}/labour-rate"),
        ("PATCH", "api/estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}/price"),
        ("PATCH", "api/estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}/material"),
        ("DELETE", "api/estimates/{id:int}/lines/{lineId:int}/materials/{materialLineId:int}"),
        ("GET", "api/estimates/{id:int}/refresh"),
        ("POST", "api/estimates/{id:int}/refresh"),
        ("PATCH", "api/estimates/{id:int}/confirm"),
        ("PATCH", "api/estimates/{id:int}/draft"),
        ("GET", "api/estimates/{id:int}/export"),
        ("PATCH", "api/estimates/{id:int}/percentages"),
        ("GET", "api/projects/{projectId:int}/estimate-names"),
        ("GET", "api/estimates/{id:int}/names"),
        ("GET", "api/team"),
        ("POST", "api/team/viewers"),
        ("DELETE", "api/team/viewers/{id:int}"),
        ("PATCH", "api/team/viewers/{id:int}/password")
    ];

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    public static TheoryData<string, string> Routes()
    {
        var data = new TheoryData<string, string>();
        foreach (var (method, route) in ProtectedRoutes)
        {
            data.Add(method, route);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task EveryProtectedEndpoint_Returns401(string method, string route)
    {
        // Every route parameter is filled with 1: the token check runs before the ID is looked up.
        var url = "/" + Regex.Replace(route, @"\{[^}]+\}", "1");
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" or "PATCH" ? new StringContent("{}", Encoding.UTF8, "application/json") : null
        };

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void TheListHoldsEveryProtectedEndpointOfTheApi()
    {
        _factory.CreateClient();
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

        var protectedInApi = endpoints
            .Where(e => e.Metadata.GetMetadata<IAuthorizeData>() is not null && e.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .SelectMany(e => e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Select(m => (m, e.RoutePattern.RawText!)))
            .OrderBy(r => r.Item2).ThenBy(r => r.m);

        Assert.Equal(ProtectedRoutes.OrderBy(r => r.Route).ThenBy(r => r.Method), protectedInApi);
    }
}
