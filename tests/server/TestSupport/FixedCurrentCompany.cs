using SmartEstimatePlatform.Api.Services;

namespace SmartEstimatePlatform.Api.Tests.TestSupport;

/// <summary>
/// A current company fixed by the test; null means nobody is logged in.
/// </summary>
public sealed class FixedCurrentCompany(int? companyId) : ICurrentCompany
{
    public int? CompanyId { get; } = companyId;
}
