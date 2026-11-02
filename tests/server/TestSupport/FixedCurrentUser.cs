using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// A current company (and optionally user) fixed by the test; null means nobody is logged in.
/// </summary>
public sealed class FixedCurrentUser(int? companyId, int? userId = null) : ICurrentUser
{
    public int? UserId { get; } = userId;

    public int? CompanyId { get; } = companyId;
}
