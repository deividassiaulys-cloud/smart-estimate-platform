using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// Lets a test choose the address a request seems to come from: the in-process test server has none. The address is
/// taken from the X-Test-Remote-Ip header, before any of the API's own middleware runs.
/// </summary>
public sealed class TestRemoteAddress : IStartupFilter
{
    public const string Header = "X-Test-Remote-Ip";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue(Header, out var value) && IPAddress.TryParse(value, out var address))
            {
                context.Connection.RemoteIpAddress = address;
            }

            await nextMiddleware(context);
        });
        next(app);
    };
}
