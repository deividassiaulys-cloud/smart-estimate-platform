using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.WorkServiceTests;

/// <summary>
/// Searching works by part of the name, ignoring letter case, through the real API.
/// </summary>
public sealed class WorkSearchTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("siena")]
    [InlineData("SIENA")]
    [InlineData("  Siena ")]
    public async Task Search_FindsEveryWorkContainingTheTextInAnyLetterCase(string search)
    {
        var company = await CompanyWithWorksAsync("Sienų dažymas", "Laikančioji siena", "Lubų dažymas");

        var found = await company.ListWorksAsync(search);

        Assert.Equal(["Laikančioji siena"], found.Select(w => w.Name));
    }

    [Fact]
    public async Task Search_ResultsAreSortedByName()
    {
        var company = await CompanyWithWorksAsync("Sienų dažymas", "Lubų dažymas", "Grindų dažymas");

        var found = await company.ListWorksAsync("dažymas");

        Assert.Equal(["Grindų dažymas", "Lubų dažymas", "Sienų dažymas"], found.Select(w => w.Name));
    }

    [Fact]
    public async Task EmptySearch_ReturnsEveryWork()
    {
        var company = await CompanyWithWorksAsync("Sienų dažymas", "Lubų dažymas");

        Assert.Equal(2, (await company.ListWorksAsync("")).Count);
        Assert.Equal(2, (await company.ListWorksAsync()).Count);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    public async Task WildcardCharacters_MatchLiterally(string search)
    {
        var company = await CompanyWithWorksAsync("Dažymas 100%", "Lubų dažymas");

        var found = await company.ListWorksAsync(search);

        Assert.Equal(search == "%" ? ["Dažymas 100%"] : [], found.Select(w => w.Name));
    }

    private async Task<LoggedInCompany> CompanyWithWorksAsync(params string[] names)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        foreach (var name in names)
        {
            await company.CreateWorkAsync(WorkRequest(name));
        }

        return company;
    }
}
