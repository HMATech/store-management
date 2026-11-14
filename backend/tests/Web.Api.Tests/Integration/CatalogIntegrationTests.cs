using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Web.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class CatalogIntegrationTests(PostgresApiFixture api)
{
    private readonly HttpClient client = api.CreateClient();

    [Theory]
    [InlineData("/brands")]
    [InlineData("/products/tags")]
    [InlineData("/products/units")]
    public async Task NamedResource_SupportsCrudAndRejectsDuplicates(string path)
    {
        var name = PostgresApiFixture.Unique("Name");
        var other = PostgresApiFixture.Unique("Other");

        var create = await client.PostAsJsonAsync(path, new { name });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = create.CreatedId();

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path, new { name })).StatusCode);
        var found = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        Assert.Equal(name, found.GetProperty("name").GetString());

        var second = await client.PostAsJsonAsync(path, new { name = other });
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PutAsJsonAsync($"{path}/{second.CreatedId()}", new { name })).StatusCode);

        var renamed = PostgresApiFixture.Unique("Renamed");
        var update = await client.PutAsJsonAsync($"{path}/{id}", new { name = renamed });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var search = await client.GetAsync($"{path}?search={renamed}");
        Assert.Contains(renamed, await search.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync($"{path}/{id}", new { name = PostgresApiFixture.Unique("Gone") })).StatusCode);
    }

    [Fact]
    public async Task Product_SupportsCrudAndPriceHistory()
    {
        var (brandId, unitId) = await CreateBrandAndUnitAsync();
        var name = PostgresApiFixture.Unique("Product");

        var create = await CreateProductAsync(name, brandId, unitId, 10m, ["Fresh"]);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.ReadJsonAsync()).GetProperty("id").GetString();

        var product = await (await client.GetAsync($"/products/{id}")).ReadJsonAsync();
        Assert.Equal(10m, product.GetProperty("price").GetDecimal());
        Assert.Equal("Fresh", product.GetProperty("tags")[0].GetString());

        var update = await client.PutAsJsonAsync($"/products/{id}", new
        {
            name,
            measure = 2f,
            measureUnitId = unitId,
            price = 12m,
            tags = new[] { "Fresh", "Local" },
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var priceChange = await client.PostAsJsonAsync($"/products/{id}/prices", new { value = 15m });
        Assert.Equal(HttpStatusCode.OK, priceChange.StatusCode);

        product = await (await client.GetAsync($"/products/{id}")).ReadJsonAsync();
        Assert.Equal(15m, product.GetProperty("price").GetDecimal());
        Assert.Equal(2, product.GetProperty("tags").GetArrayLength());

        var prices = await (await client.GetAsync($"/products/prices?search={name}&sortOrder=DESC")).ReadJsonAsync();
        var history = prices.GetProperty("responses");
        Assert.Equal(3, prices.GetProperty("totalCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, history[0].GetProperty("validToTime").ValueKind);

        var currentPriceId = history[0].GetProperty("id").GetString();
        var oldPriceId = history[2].GetProperty("id").GetString();
        var deleteCurrent = await client.DeleteAsync($"/products/prices/{currentPriceId}");
        Assert.Equal(HttpStatusCode.Conflict, deleteCurrent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/products/prices/{oldPriceId}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/products/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/products/{id}")).StatusCode);
    }

    [Fact]
    public async Task Price_CanBeReadAndCorrected()
    {
        var (brandId, unitId) = await CreateBrandAndUnitAsync();
        var name = PostgresApiFixture.Unique("Product");
        await CreateProductAsync(name, brandId, unitId, 10m, []);
        var prices = await (await client.GetAsync($"/products/prices?search={name}")).ReadJsonAsync();
        var path = $"/products/prices/{prices.GetProperty("responses")[0].GetProperty("id").GetString()}";

        var update = await client.PutAsJsonAsync(path, new { value = 11m });
        var read = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(11m, (await read.ReadJsonAsync()).GetProperty("value").GetDecimal());
    }

    [Fact]
    public async Task Price_WithUnknownId_ReturnsNotFound()
    {
        var path = $"/products/prices/{Guid.NewGuid()}";

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PutAsJsonAsync(path, new { value = 1m })).StatusCode);
    }

    [Fact]
    public async Task Product_Create_WithDuplicateOrUnknownReferences_ReturnsError()
    {
        var (brandId, unitId) = await CreateBrandAndUnitAsync();
        var name = PostgresApiFixture.Unique("Product");
        await CreateProductAsync(name, brandId, unitId, 1m, []);

        Assert.Equal(HttpStatusCode.Conflict, (await CreateProductAsync(name, brandId, unitId, 1m, [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CreateProductAsync("x", 999999, unitId, 1m, [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CreateProductAsync("x", brandId, 999999, 1m, [])).StatusCode);
    }

    [Fact]
    public async Task Product_Update_ChangesDescription()
    {
        var (brandId, unitId) = await CreateBrandAndUnitAsync();
        var name = PostgresApiFixture.Unique("Product");
        var created = await (await CreateProductAsync(name, brandId, unitId, 1m, [])).ReadJsonAsync();
        var path = $"/products/{created.GetProperty("id").GetString()}";

        var update = await client.PutAsJsonAsync(path, new
        {
            name,
            description = "Updated",
            measure = 1f,
            measureUnitId = unitId,
            price = 1m,
            tags = Array.Empty<string>(),
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var read = await (await client.GetAsync(path)).ReadJsonAsync();
        Assert.Equal("Updated", read.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Brand_Update_KeepingItsName_Succeeds()
    {
        var name = PostgresApiFixture.Unique("Brand");
        var brand = await client.PostAsJsonAsync("/brands", new { name });

        var update = await client.PutAsJsonAsync($"/brands/{brand.CreatedId()}", new { name });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task DeletingUnitInUse_ReturnsConflict()
    {
        var (brandId, unitId) = await CreateBrandAndUnitAsync();
        await CreateProductAsync(PostgresApiFixture.Unique("Product"), brandId, unitId, 1m, []);

        var response = await client.DeleteAsync($"/products/units/{unitId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadJsonAsync();
        Assert.Equal("Products.UnitIsStillReferenced", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeletingBrandInUse_ReturnsConflict()
    {
        var (brandId, unitId) = await CreateBrandAndUnitAsync();
        await CreateProductAsync(PostgresApiFixture.Unique("Product"), brandId, unitId, 1m, []);

        var response = await client.DeleteAsync($"/brands/{brandId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PageBelowOne_ReturnsBadRequest()
    {
        var response = await client.GetAsync("/brands?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/products")]
    [InlineData("/stocks")]
    public async Task ProductSearch_IgnoresLetterCaseAndOuterSpaces(string path)
    {
        var (brandId, unitId) = await CreateBrandAndUnitAsync();
        var name = PostgresApiFixture.Unique("Indomie Goreng");
        await CreateProductAsync(name, brandId, unitId, 3500m, []);

        foreach (var search in new[] { name.ToLowerInvariant(), name.ToUpperInvariant(), $"  {name}  " })
        {
            var page = await (await client.GetAsync($"{path}?search={Uri.EscapeDataString(search)}")).ReadJsonAsync();

            Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        }
    }

    [Theory]
    [InlineData("/brands")]
    [InlineData("/products/tags")]
    [InlineData("/products/units")]
    public async Task NamedResourceSearch_IgnoresLetterCase(string path)
    {
        var name = PostgresApiFixture.Unique("Sembako");
        await client.PostAsJsonAsync(path, new { name });

        foreach (var search in new[] { name.ToLowerInvariant(), name.ToUpperInvariant() })
        {
            var body = await (await client.GetAsync($"{path}?search={Uri.EscapeDataString(search)}")).ReadJsonAsync();
            var items = body.ValueKind == JsonValueKind.Array ? body : body.GetProperty("responses");

            Assert.Equal(1, items.GetArrayLength());
        }
    }

    private async Task<(int BrandId, int UnitId)> CreateBrandAndUnitAsync()
    {
        var brand = await client.PostAsJsonAsync("/brands", new { name = PostgresApiFixture.Unique("Brand") });
        var unit = await client.PostAsJsonAsync("/products/units", new { name = PostgresApiFixture.Unique("Unit") });

        return (int.Parse(brand.CreatedId()), int.Parse(unit.CreatedId()));
    }

    private Task<HttpResponseMessage> CreateProductAsync(string name, int brandId, int unitId, decimal price,
        string[] tags)
    {
        return client.PostAsJsonAsync("/products", new
        {
            name,
            description = "Description",
            brandId,
            measure = 1f,
            measureUnitId = unitId,
            price,
            tags,
        });
    }
}
