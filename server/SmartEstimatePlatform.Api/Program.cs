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

// Add services to the container.

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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, LoggingAuthorizationResultHandler>();

var app = builder.Build();

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

app.Run();

// Lets the integration tests start the API in-process (WebApplicationFactory<Program>).
public partial class Program;
