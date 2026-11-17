using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using SmartEstimatePlatform.Api.Models.DTOs.Estimates;
using SmartEstimatePlatform.Api.Models.Entities;
using SmartEstimatePlatform.Api.Tests.TestSupport;
using static SmartEstimatePlatform.Api.Tests.TestSupport.CatalogRequests;

namespace SmartEstimatePlatform.Api.Tests.EstimateServiceTests;

/// <summary>
/// The names-only view of estimates (FR-026): a Viewer sees work and material names and never receives
/// prices, quantities, norms, percentages, totals, units or status, by any means (BR-005, NFR-005).
/// </summary>
public sealed class EstimateNamesTests : IDisposable
{
    private static readonly string[] AllowedJsonProperties = ["id", "name", "works", "workName", "materialNames"];

    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Viewer_ListsEveryEstimateByNameWhateverItsStatus()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var project = await company.Client.CreateProjectAsync("Buto remontas");
        var draft = await CreateInProjectAsync(company, project.Id, "Virtuvė");
        var confirmed = await CreateInProjectAsync(company, project.Id, "Pradinė sąmata");
        await company.Client.ConfirmEstimateAsync(confirmed.Id);
        var viewer = await _factory.CreateViewerClientAsync(company);

        var response = await viewer.GetAsync($"/api/projects/{project.Id}/estimate-names");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = (await response.Content.ReadFromJsonAsync<List<EstimateNameResponse>>())!;
        Assert.Equal([(confirmed.Id, "Pradinė sąmata"), (draft.Id, "Virtuvė")], list.Select(e => (e.Id, e.Name)));
        AssertOnlyNameProperties(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Viewer_OpensAnEstimateAndGetsOnlyNamesInOrder()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var workId = await company.CreateWallWorkAsync();
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, workId, 35m);
        await company.Client.PatchPercentagesAsync(estimate.Id, 10m, 15m, 21m);
        var viewer = await _factory.CreateViewerClientAsync(company);

        var response = await viewer.GetAsync($"/api/estimates/{estimate.Id}/names");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var names = (await response.Content.ReadFromJsonAsync<EstimateNamesResponse>())!;
        Assert.Equal("Pradinė sąmata", names.Name);
        var work = Assert.Single(names.Works);
        Assert.Equal("Siena A++", work.WorkName);
        Assert.Equal(["Gipso plokštė", "Varžtai", "Plėvelė"], work.MaterialNames);
        var json = await response.Content.ReadAsStringAsync();
        AssertOnlyNameProperties(json);
        Assert.DoesNotContain("1263", json);
        Assert.DoesNotContain("35", json);
    }

    [Fact]
    public async Task RepeatedMaterial_IsShownEachTime()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var paint = await company.CreateMaterialAsync("Dažai", "l");
        var work = await company.CreateWorkAsync(WorkRequest("Sienų dažymas", "m²", (paint.Id, 0.150m, 5), (paint.Id, 0.050m, 0)));
        var estimate = await company.CreateEstimateAsync();
        await company.Client.AddWorkLineAsync(estimate.Id, work.Id, 10m);
        var viewer = await _factory.CreateViewerClientAsync(company);

        var names = (await viewer.GetFromJsonAsync<EstimateNamesResponse>($"/api/estimates/{estimate.Id}/names"))!;

        Assert.Equal(["Dažai", "Dažai"], Assert.Single(names.Works).MaterialNames);
    }

    [Fact]
    public async Task Viewer_IsStillForbiddenFromTheManagersEstimateData()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var estimate = await company.CreateEstimateAsync();
        var viewer = await _factory.CreateViewerClientAsync(company);

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"/api/estimates/{estimate.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"/api/projects/{estimate.ProjectId}/estimates")).StatusCode);
    }

    [Fact]
    public async Task Manager_CanUseTheNamesEndpointsToo()
    {
        var company = await _factory.RegisterCompanyAsync("Alpha UAB", "alpha@example.com");
        var estimate = await company.CreateEstimateAsync();

        Assert.Equal(HttpStatusCode.OK, (await company.Client.GetAsync($"/api/estimates/{estimate.Id}/names")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await company.Client.GetAsync($"/api/projects/{estimate.ProjectId}/estimate-names")).StatusCode);
    }

    [Fact]
    public async Task AnonymousRequests_AreUnauthorized()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/estimates/1/names")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/projects/1/estimate-names")).StatusCode);
    }

    [Fact]
    public void NamesResponses_CannotCarryNumbersOrStatus()
    {
        // Structural guarantee: these types have nowhere to put a price, quantity, norm, total or status.
        Type[] types = [typeof(EstimateNameResponse), typeof(EstimateNamesResponse), typeof(WorkNamesResponse)];

        var offending = types
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance), (t, p) => (Type: t, Property: p))
            .Where(x => IsNumberOrStatus(x.Property.PropertyType))
            .Where(x => !(x.Type == typeof(EstimateNameResponse) && x.Property.Name == nameof(EstimateNameResponse.Id)))
            .Select(x => $"{x.Type.Name}.{x.Property.Name}");

        Assert.Empty(offending);
    }

    private static bool IsNumberOrStatus(Type type)
    {
        var element = type.IsGenericType ? type.GetGenericArguments()[0] : type;
        element = Nullable.GetUnderlyingType(element) ?? element;
        return element == typeof(EstimateStatus) || element.IsEnum
               || element == typeof(decimal) || element == typeof(double) || element == typeof(float)
               || element == typeof(int) || element == typeof(long) || element == typeof(short);
    }

    // Every property name anywhere in the JSON must be one of the name-only properties.
    private static void AssertOnlyNameProperties(string json)
    {
        var names = new HashSet<string>();
        Collect(JsonDocument.Parse(json).RootElement, names);
        Assert.Subset(AllowedJsonProperties.ToHashSet(), names);

        static void Collect(JsonElement element, HashSet<string> names)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    names.Add(property.Name);
                    Collect(property.Value, names);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, names);
                }
            }
        }
    }

    private static async Task<EstimateResponse> CreateInProjectAsync(LoggedInCompany company, int projectId, string name)
    {
        var response = await company.Client.PostAsJsonAsync($"/api/projects/{projectId}/estimates", new CreateEstimateRequest { Name = name });
        return (await response.Content.ReadFromJsonAsync<EstimateResponse>())!;
    }
}
