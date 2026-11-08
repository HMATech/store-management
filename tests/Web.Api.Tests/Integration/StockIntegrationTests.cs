using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Web.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class StockIntegrationTests(PostgresApiFixture api)
{
    private const string OccurredAt = "2026-01-10T12:00:00Z";

    private readonly HttpClient client = api.CreateClient();

    [Fact]
    public async Task Stock_CombinesPurchasesSalesAndAdjustmentsAcrossPriceLines()
    {
        var product = await CreateProductAsync();
        await CreateTransactionAsync("/purchases", product, 10);
        await client.PostAsJsonAsync($"/products/{product.Id}/prices", new { value = 12m });
        await CreateTransactionAsync("/purchases", product, 4);
        await CreateTransactionAsync("/sales", product, 3);
        await CreateAdjustmentAsync(product.Id, -2);

        var stock = await GetStockAsync(product.Id);

        Assert.Equal(product.Name, stock.GetProperty("productName").GetString());
        Assert.Equal(14, stock.GetProperty("purchased").GetInt64());
        Assert.Equal(3, stock.GetProperty("sold").GetInt64());
        Assert.Equal(-2, stock.GetProperty("adjusted").GetInt64());
        Assert.Equal(9, stock.GetProperty("quantity").GetInt64());
    }

    [Fact]
    public async Task Stock_OfProductWithoutMovements_IsZero()
    {
        var product = await CreateProductAsync();

        var stock = await GetStockAsync(product.Id);

        Assert.Equal(0, stock.GetProperty("purchased").GetInt64());
        Assert.Equal(0, stock.GetProperty("sold").GetInt64());
        Assert.Equal(0, stock.GetProperty("adjusted").GetInt64());
        Assert.Equal(0, stock.GetProperty("quantity").GetInt64());
    }

    [Fact]
    public async Task Stock_AboveIntMaxValue_DoesNotOverflow()
    {
        var product = await CreateProductAsync();
        await CreateTransactionAsync("/purchases", product, 2_000_000_000);
        await CreateTransactionAsync("/purchases", product, 2_000_000_000);

        var stock = await GetStockAsync(product.Id);

        Assert.Equal(4_000_000_000L, stock.GetProperty("quantity").GetInt64());
    }

    [Fact]
    public async Task Stock_ForUnknownProduct_ReturnsNotFound()
    {
        var response = await client.GetAsync($"/stocks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StockList_FiltersBySearch()
    {
        var product = await CreateProductAsync();
        await CreateTransactionAsync("/purchases", product, 7);

        var page = await (await client.GetAsync($"/stocks?search={product.Name}")).ReadJsonAsync();

        var stock = Assert.Single(page.GetProperty("responses").EnumerateArray());
        Assert.Equal(product.Id, stock.GetProperty("productId").GetString());
        Assert.Equal(7, stock.GetProperty("quantity").GetInt64());
    }

    [Fact]
    public async Task AdjustmentLifecycle_CreatesListsGetsAndDeletes()
    {
        var product = await CreateProductAsync();

        var id = await CreateAdjustmentAsync(product.Id, 5);
        var path = $"/stocks/adjustments/{id}";

        var adjustment = await (await client.GetAsync(path)).ReadJsonAsync();
        Assert.Equal(5, adjustment.GetProperty("quantity").GetInt32());
        Assert.Equal("Stock count", adjustment.GetProperty("reason").GetString());

        var list = await (await client.GetAsync($"/stocks/adjustments?productId={product.Id}")).ReadJsonAsync();
        Assert.Equal(id, Assert.Single(list.GetProperty("responses").EnumerateArray()).GetProperty("id").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(path)).StatusCode);
        Assert.Equal(0, (await GetStockAsync(product.Id)).GetProperty("quantity").GetInt64());
    }

    [Fact]
    public async Task Adjustment_ForUnknownProduct_ReturnsNotFound()
    {
        var response = await client.PostAsJsonAsync("/stocks/adjustments",
            new { productId = Guid.NewGuid(), quantity = 1, reason = "Count", occurrenceTime = OccurredAt });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Products.NotFound", (await response.ReadJsonAsync()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeletingProductWithAdjustment_ReturnsConflict()
    {
        var product = await CreateProductAsync();
        await CreateAdjustmentAsync(product.Id, 1);

        var response = await client.DeleteAsync($"/products/{product.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Products.InUse", (await response.ReadJsonAsync()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Product_WithMinimumOfZero_KeepsZeroInTheDatabase()
    {
        var product = await CreateProductAsync(new { minimumQuantity = 0 });

        var body = await (await client.GetAsync($"/products/{product.Id}")).ReadJsonAsync();

        Assert.Equal(0, body.GetProperty("stockDefinition").GetProperty("minimumQuantity").GetInt32());
        Assert.Equal(0, (await GetStockAsync(product.Id)).GetProperty("minimumQuantity").GetInt32());
    }

    [Fact]
    public async Task Product_WithoutStockDefinition_GetsTheDefault()
    {
        var product = await CreateProductAsync();

        var stock = await GetStockAsync(product.Id);

        Assert.Equal(5, stock.GetProperty("minimumQuantity").GetInt32());
        Assert.Equal("Low", stock.GetProperty("status").GetString());
    }

    [Fact]
    public async Task StockList_FiltersByStatusInSql()
    {
        var product = await CreateProductAsync(new { minimumQuantity = 2 });
        await CreateTransactionAsync("/purchases", product, 10);

        Assert.Equal(1, await CountAsync($"search={product.Name}&status=normal"));
        Assert.Equal(0, await CountAsync($"search={product.Name}&status=low"));

        await CreateTransactionAsync("/sales", product, 9);
        Assert.Equal(1, await CountAsync($"search={product.Name}&status=low"));

        await CreateTransactionAsync("/sales", product, 2);
        Assert.Equal(1, await CountAsync($"search={product.Name}&status=negative"));
        Assert.Equal("Negative", (await GetStockAsync(product.Id)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task UpdatingProductsMinimum_ChangesItsStatus()
    {
        var product = await CreateProductAsync(new { minimumQuantity = 2 });
        await CreateTransactionAsync("/purchases", product, 4);

        var response = await client.PutAsJsonAsync($"/products/{product.Id}", new
        {
            name = product.Name,
            measure = 1f,
            measureUnitId = await UnitIdOfAsync(product.Id),
            price = 10m,
            tags = Array.Empty<string>(),
            stockDefinition = new { minimumQuantity = 6 },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Low", (await GetStockAsync(product.Id)).GetProperty("status").GetString());
    }

    private async Task<int> CountAsync(string query)
    {
        var response = await client.GetAsync($"/stocks?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("totalCount").GetInt32();
    }

    private async Task<int> UnitIdOfAsync(string productId)
    {
        var body = await (await client.GetAsync($"/products/{productId}")).ReadJsonAsync();
        return body.GetProperty("measureUnit").GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> GetStockAsync(string productId)
    {
        var response = await client.GetAsync($"/stocks/{productId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    private async Task<string> CreateAdjustmentAsync(string productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/stocks/adjustments",
            new { productId, quantity, reason = "Stock count", occurrenceTime = OccurredAt });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response.CreatedId();
    }

    private async Task CreateTransactionAsync(string path, (string Id, string Name) product, int quantity)
    {
        var response = await client.PostAsJsonAsync(path, new
        {
            title = PostgresApiFixture.Unique("Title"),
            tags = Array.Empty<string>(),
            productEntries = new[] { new { id = product.Id, quantity, unitCost = 1m } },
            occurrenceTime = OccurredAt,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<(string Id, string Name)> CreateProductAsync(object? stockDefinition = null)
    {
        var name = PostgresApiFixture.Unique("Product");
        var brand = await client.PostAsJsonAsync("/brands", new { name = PostgresApiFixture.Unique("Brand") });
        var unit = await client.PostAsJsonAsync("/products/units", new { name = PostgresApiFixture.Unique("Unit") });
        var product = await client.PostAsJsonAsync("/products", new
        {
            name,
            brandId = int.Parse(brand.CreatedId()),
            measure = 1f,
            measureUnitId = int.Parse(unit.CreatedId()),
            price = 10m,
            tags = Array.Empty<string>(),
            stockDefinition,
        });

        Assert.Equal(HttpStatusCode.Created, product.StatusCode);
        return ((await product.ReadJsonAsync()).GetProperty("id").GetString()!, name);
    }
}
