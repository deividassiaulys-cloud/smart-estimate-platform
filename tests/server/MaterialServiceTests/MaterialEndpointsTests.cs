using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using SmartEstimatePlatform.Api.Models.DTOs;
using SmartEstimatePlatform.Api.Tests.TestSupport;

namespace SmartEstimatePlatform.Api.Tests.MaterialServiceTests;

/// <summary>
/// Integration tests for the material catalog's validation, editing and deleting, through the real API.
/// </summary>
public sealed class MaterialEndpointsTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Theory]
    [InlineData("0")]
    [InlineData("12.34")]
    public async Task Create_ValidMaterial_IsSavedWithATrimmedName(string price)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await CreateAsync(company, "  Cementas  ", "kg", decimal.Parse(price, CultureInfo.InvariantCulture));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var material = Assert.Single(await ListAsync(company));
        Assert.Equal("Cementas", material.Name);
        Assert.Equal("kg", material.Unit);
        Assert.Equal(decimal.Parse(price, CultureInfo.InvariantCulture), material.Price);
    }

    [Theory]
    [InlineData("-0.01", "kg")]
    [InlineData("12.345", "kg")]
    [InlineData("10000000000000000.00", "kg")]
    [InlineData("1", "cm")]
    [InlineData("1", "m2")]
    [InlineData("1", "")]
    public async Task Create_InvalidPriceOrUnit_IsRejected(string price, string unit)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await CreateAsync(company, "Cementas", unit, decimal.Parse(price, CultureInfo.InvariantCulture));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListAsync(company));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyName_IsRejected(string name)
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var response = await CreateAsync(company, name, "kg", 1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListAsync(company));
    }

    [Fact]
    public async Task Create_SameNameAndUnitInAnyLetterCase_IsRejectedAsDuplicate()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await CreateAsync(company, "Cementas", "kg", 1);

        var response = await CreateAsync(company, "cementas ", "kg", 2);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(await ListAsync(company));
    }

    [Fact]
    public async Task Create_SameNameWithADifferentUnit_IsAccepted()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await CreateAsync(company, "Cementas", "kg", 1);

        var response = await CreateAsync(company, "Cementas", "t", 150);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(2, (await ListAsync(company)).Count);
    }

    [Fact]
    public async Task Update_ChangesNameUnitAndPrice()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var created = await ReadAsync(await CreateAsync(company, "Cementas", "kg", 1));

        var response = await company.Client.PutAsJsonAsync($"/api/materials/{created.Id}",
            new MaterialRequest { Name = "Smėlis ", Unit = "t", Price = 25.5m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var material = await company.Client.GetFromJsonAsync<MaterialResponse>($"/api/materials/{created.Id}");
        Assert.Equal(("Smėlis", "t", 25.5m), (material!.Name, material.Unit, material.Price));
    }

    [Fact]
    public async Task Update_ToAnotherMaterialsNameAndUnit_IsRejectedAsDuplicate()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        await CreateAsync(company, "Cementas", "kg", 1);
        var sand = await ReadAsync(await CreateAsync(company, "Smėlis", "kg", 2));

        var response = await company.Client.PutAsJsonAsync($"/api/materials/{sand.Id}",
            new MaterialRequest { Name = "CEMENTAS", Unit = "kg", Price = 2 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var unchanged = await company.Client.GetFromJsonAsync<MaterialResponse>($"/api/materials/{sand.Id}");
        Assert.Equal("Smėlis", unchanged!.Name);
    }

    [Fact]
    public async Task Update_KeepingItsOwnNameAndUnit_IsAccepted()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var created = await ReadAsync(await CreateAsync(company, "Cementas", "kg", 1));

        var response = await company.Client.PutAsJsonAsync($"/api/materials/{created.Id}",
            new MaterialRequest { Name = "cementas", Unit = "kg", Price = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidPrice_IsRejected()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var created = await ReadAsync(await CreateAsync(company, "Cementas", "kg", 1));

        var response = await company.Client.PutAsJsonAsync($"/api/materials/{created.Id}",
            new MaterialRequest { Name = "Cementas", Unit = "kg", Price = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, (await ListAsync(company)).Single().Price);
    }

    [Fact]
    public async Task Delete_RemovesTheMaterial()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var created = await ReadAsync(await CreateAsync(company, "Cementas", "kg", 1));

        var response = await company.Client.DeleteAsync($"/api/materials/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await ListAsync(company));
    }

    [Fact]
    public async Task GetUpdateOrDelete_MissingId_ReturnsNotFound()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var get = await company.Client.GetAsync("/api/materials/999999");
        var update = await company.Client.PutAsJsonAsync("/api/materials/999999",
            new MaterialRequest { Name = "Cementas", Unit = "kg", Price = 1 });
        var delete = await company.Client.DeleteAsync("/api/materials/999999");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
    }

    [Fact]
    public async Task Units_ReturnsTheFixedList()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");

        var units = await company.Client.GetFromJsonAsync<List<string>>("/api/units");

        Assert.Equal(["vnt.", "m", "m²", "m³", "kg", "t", "l", "val.", "kompl."], units);
    }

    private static Task<HttpResponseMessage> CreateAsync(LoggedInCompany company, string name, string unit, decimal price) =>
        company.Client.PostAsJsonAsync("/api/materials", new MaterialRequest { Name = name, Unit = unit, Price = price });

    private static async Task<MaterialResponse> ReadAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<MaterialResponse>())!;

    private static async Task<List<MaterialResponse>> ListAsync(LoggedInCompany company) =>
        (await company.Client.GetFromJsonAsync<List<MaterialResponse>>("/api/materials"))!;
}
