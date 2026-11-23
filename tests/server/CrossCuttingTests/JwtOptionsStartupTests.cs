using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.CrossCuttingTests;

/// <summary>
/// The application does not start with a signing key that is too short for HMAC-SHA256 (feature 021, F3).
/// </summary>
public sealed class JwtOptionsStartupTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public void ShortKey_StopsTheApp()
    {
        using var shortKey = _factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:Key", new string('k', 31)));

        var error = Assert.ThrowsAny<Exception>(() => shortKey.CreateClient());

        var validation = FindInner<OptionsValidationException>(error);
        Assert.NotNull(validation);
        Assert.Contains("Jwt:Key must be configured and at least 32 characters long.", validation.Message);
    }

    [Fact]
    public void KeyOf32Characters_Starts()
    {
        using var minimalKey = _factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:Key", new string('k', 32)));

        Assert.NotNull(minimalKey.CreateClient());
    }

    private static T? FindInner<T>(Exception? exception) where T : Exception
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is T match)
            {
                return match;
            }
        }

        return null;
    }
}
