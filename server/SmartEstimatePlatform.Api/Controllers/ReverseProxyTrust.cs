using Microsoft.AspNetCore.HttpOverrides;

namespace SmartEstimatePlatform.Api.Controllers;

/// <summary>
/// Behind the reverse proxy (Caddy, feature 022) every request arrives from the proxy. The proxy's X-Forwarded-For
/// and X-Forwarded-Proto headers are trusted only from the network in "ReverseProxy:TrustedNetwork" (a CIDR such as
/// 172.30.0.0/24) and for one hop, so the rate limit sees the real client and HTTPS is recognised.
/// </summary>
public static class ReverseProxyTrust
{
    private const string SettingName = "ReverseProxy:TrustedNetwork";

    /// <summary>
    /// Reads the trusted network. Outside Development it is required, as AllowedHosts is: without it every client
    /// would share the proxy's address in the login rate limit. In Development it is optional.
    /// Returns whether forwarded headers are used.
    /// </summary>
    public static bool AddReverseProxyTrust(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var setting = configuration[SettingName];
        if (string.IsNullOrEmpty(setting) && environment.IsDevelopment())
        {
            return false;
        }

        if (!System.Net.IPNetwork.TryParse(setting, out var network))
        {
            throw new InvalidOperationException(
                $"{SettingName} must name the reverse proxy's network as a CIDR, e.g. 172.30.0.0/24, outside Development " +
                "(set the ReverseProxy__TrustedNetwork environment variable).");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.KnownIPNetworks.Add(network);
        });
        return true;
    }
}
