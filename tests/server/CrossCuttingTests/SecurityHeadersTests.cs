using System.Net;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// Every API response tells the browser not to sniff types, not to frame it, not to send referrers and not to run
/// anything from it (feature 021, F7) — also error responses, which go through the exception handler.
/// </summary>
public sealed class SecurityHeadersTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task EveryResponse_HasTheSecurityHeaders()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var ok = await company.Client.GetAsync("/api/units");
        var unauthorized = await _factory.CreateClient().GetAsync("/api/units");
        var notFound = await company.Client.GetAsync("/api/materials/999");

        Assert.Equal(
            (HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound),
            (ok.StatusCode, unauthorized.StatusCode, notFound.StatusCode));
        Assert.All([ok, unauthorized, notFound], response =>
        {
            Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
            Assert.Equal("DENY", Header(response, "X-Frame-Options"));
            Assert.Equal("no-referrer", Header(response, "Referrer-Policy"));
            Assert.Equal("default-src 'none'; frame-ancestors 'none'", Header(response, "Content-Security-Policy"));
        });
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
}
