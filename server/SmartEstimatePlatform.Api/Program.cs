using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartEstimatePlatform.Api.Controllers;
using SmartEstimatePlatform.Api.Data;
using SmartEstimatePlatform.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Outside development the API must name the hosts it answers for (feature 021, F7): the environment variable
// AllowedHosts, e.g. "estimates.example.lt". "*" is only for local runs.
if (!builder.Environment.IsDevelopment() && builder.Configuration["AllowedHosts"] is null or "" or "*")
{
    throw new InvalidOperationException(
        "AllowedHosts must list the API's host names outside Development (set the AllowedHosts environment variable).");
}

// Outside development the console log is JSON, one entry per line, with the request's scopes (TraceId = the
// correlation id, RequestPath) on every entry, so the server's logs can be searched by request (feature 022).
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.UseUtcTimestamp = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    });
}

// Add services to the container.

// Forwarded headers only from the reverse proxy's network (feature 022); required outside Development.
var behindReverseProxy = builder.Services.AddReverseProxyTrust(builder.Configuration, builder.Environment);

// The connection string is supplied outside the repository (user secrets or environment variables).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion));

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => o.Key.Length >= 32, "Jwt:Key must be configured and at least 32 characters long.")
    .ValidateOnStart();

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            // The user is checked on every request, so a removed user's token stops working immediately,
            // the token's company must be the user's real company (it is the only source of the current company),
            // and a password reset or a logout invalidates every token issued before it.
            OnTokenValidated = async context =>
            {
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var sub = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var company = context.Principal?.FindFirst(TokenService.CompanyIdClaim)?.Value;
                var version = context.Principal?.FindFirst(TokenService.TokenVersionClaim)?.Value;
                if (!int.TryParse(sub, out var userId)
                    || !int.TryParse(company, out var companyId)
                    || !int.TryParse(version, out var tokenVersion)
                    || !await authService.IsTokenUserValidAsync(userId, companyId, tokenVersion))
                {
                    context.Fail("The token's user no longer exists, has another company, or has reset their password.");
                }
            }
        };
    });

builder.Services.AddControllers();
builder.Services.AddAuthRateLimiting(builder.Configuration);
builder.Services.AddHealthEndpoints();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, LoggingAuthorizationResultHandler>();

var app = builder.Build();

// First, so HSTS, HTTPS redirection, the rate limiter and logging all see the real client address and scheme.
if (behindReverseProxy)
{
    app.UseForwardedHeaders();
}

// Security headers on every response, also errors (feature 021, F7). Added when the response starts, so the
// exception handler, which clears the headers of a failed response, cannot remove them.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        return Task.CompletedTask;
    });
    await next();
});

// The correlation id (feature 022): the request's trace id, which every log entry of the request carries in its
// TraceId scope and the ProblemDetails traceId contains. Added when the response starts, like the headers above.
app.Use(async (context, next) =>
{
    var correlationId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        return Task.CompletedTask;
    });
    await next();
});

app.UseExceptionHandler();

// Outside development, browsers are told to keep using HTTPS for this host (NFR-001).
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthEndpoints();

app.Run();

// Lets the integration tests start the API in-process (WebApplicationFactory<Program>).
public partial class Program;
