using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Web.Api.Tests.Integration;

/// <summary>
/// Purchases and sales share most of their contract, so most tests run against both. Purchases record the unit
/// cost paid (<c>unitCost</c>, <c>totalCost</c>) while sales keep the selling price (<c>priceValue</c>,
/// <c>totalPrice</c>); the shared tests send a unit cost equal to the product's price so the totals match.
/// </summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class TransactionIntegrationTests(PostgresApiFixture api)
{
    private const string OccurredAt = "2026-01-10T12:00:00Z";
    private const string Purchases = "/purchases";

    private readonly HttpClient client = api.CreateClient();

    public static TheoryData<string> Resources => new() { Purchases, "/sales" };

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Create_ThenGetById_ReturnsEntriesAndTotal(string path)
    {
        var product = await CreateProductAsync(10m);

        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 3, 10m);

        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        Assert.Equal(30m, Total(path, body));
        var entry = body.GetProperty("productEntries")[0];
        Assert.Equal(product, entry.GetProperty("productId").GetString());
        Assert.Equal(10m, entry.GetProperty(path == Purchases ? "unitCost" : "priceValue").GetDecimal());
        Assert.Equal(3, entry.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task SaleEntries_KeepThePriceFromWhenTheyWereCreated()
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync("/sales", PostgresApiFixture.Unique("Title"), product, quantity: 1, 10m);

        await client.PostAsJsonAsync($"/products/{product}/prices", new { value = 99m });

        var body = await (await client.GetAsync($"/sales/{id}")).ReadJsonAsync();
        Assert.Equal(10m, body.GetProperty("totalPrice").GetDecimal());
    }

    [Fact]
    public async Task Sale_WithoutTitle_IsCreatedAndUpdatedWithAnEmptyTitle()
    {
        var product = await CreateProductAsync(10m);

        var created = await client.PostAsJsonAsync("/sales", new
        {
            tags = Array.Empty<string>(),
            productEntries = new[] { new { id = product, quantity = 1 } },
            occurrenceTime = OccurredAt,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = created.CreatedId();
        Assert.Equal("", (await (await client.GetAsync($"/sales/{id}")).ReadJsonAsync())
            .GetProperty("title").GetString());

        var updated = await client.PutAsJsonAsync($"/sales/{id}/details",
            new { tags = Array.Empty<string>(), occurrenceTime = OccurredAt });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
    }

    [Fact]
    public async Task Purchase_WithoutTitle_ReturnsBadRequest()
    {
        var product = await CreateProductAsync(10m);

        var response = await client.PostAsJsonAsync(Purchases, new
        {
            tags = Array.Empty<string>(),
            productEntries = new[] { new { id = product, quantity = 1, unitCost = 7m } },
            occurrenceTime = OccurredAt,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_RecordsUnitCostSeparatelyFromSellingPrice()
    {
        var product = await CreateProductAsync(10m);

        var id = await CreateTransactionAsync(Purchases, PostgresApiFixture.Unique("Title"), product, quantity: 3, 7m);

        var body = await (await client.GetAsync($"{Purchases}/{id}")).ReadJsonAsync();
        Assert.Equal(21m, body.GetProperty("totalCost").GetDecimal());
        var entry = body.GetProperty("productEntries")[0];
        Assert.Equal(7m, entry.GetProperty("unitCost").GetDecimal());
        Assert.Equal(21m, entry.GetProperty("totalCost").GetDecimal());
        Assert.False(entry.TryGetProperty("priceId", out _));
    }

    [Fact]
    public async Task Purchase_WithSameProductAtTwoCosts_KeepsTwoLines()
    {
        var product = await CreateProductAsync(10m);
        var response = await client.PostAsJsonAsync(Purchases, new
        {
            title = PostgresApiFixture.Unique("Title"),
            tags = Array.Empty<string>(),
            productEntries = new[]
            {
                new { id = product, quantity = 2, unitCost = 6m },
                new { id = product, quantity = 1, unitCost = 5.5m },
            },
            occurrenceTime = OccurredAt,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await (await client.GetAsync($"{Purchases}/{response.CreatedId()}")).ReadJsonAsync();
        Assert.Equal(2, body.GetProperty("productEntries").GetArrayLength());
        Assert.Equal(17.5m, body.GetProperty("totalCost").GetDecimal());
    }

    [Fact]
    public async Task Purchase_UpdateEntries_CorrectsTheUnitCost()
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(Purchases, PostgresApiFixture.Unique("Title"), product, quantity: 2, 6m);
        var entryId = await FirstEntryIdAsync(Purchases, id);

        var changed = await client.PutAsJsonAsync($"{Purchases}/{id}/entries", new
        {
            productEntries = new[] { new { id = entryId, productId = product, quantity = 2, unitCost = 6.5m } },
        });

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(13m, (await changed.ReadJsonAsync()).GetProperty("totalCost").GetDecimal());
    }

    [Fact]
    public async Task Purchase_WithNegativeUnitCost_ReturnsBadRequest()
    {
        var product = await CreateProductAsync(10m);

        var response = await client.PostAsJsonAsync(Purchases, new
        {
            title = "Title",
            tags = Array.Empty<string>(),
            productEntries = new[] { new { id = product, quantity = 1, unitCost = -1m } },
            occurrenceTime = OccurredAt,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Get_FiltersBySearchDateAndTotalInSql(string path)
    {
        var title = PostgresApiFixture.Unique("Title");
        var product = await CreateProductAsync(10m);
        await CreateTransactionAsync(path, title, product, quantity: 3, 10m);

        Assert.Equal(1, await CountAsync(path, $"search={title.ToUpperInvariant()}"));
        Assert.Equal(1, await CountAsync(path, $"search={title}&totalFrom=30&totalTo=30"));
        Assert.Equal(0, await CountAsync(path, $"search={title}&totalFrom=31"));
        Assert.Equal(0, await CountAsync(path, $"search={title}&totalTo=29"));
        Assert.Equal(1, await CountAsync(path, $"search={title}&occurenceFrom=2026-01-10&occurenceTo=2026-01-10"));
        Assert.Equal(0, await CountAsync(path, $"search={title}&occurenceFrom=2026-01-11"));
        Assert.Equal(0, await CountAsync(path, $"search={title}&occurenceTo=2026-01-09"));
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task UpdateDetails_ChangesTitleAndTags(string path)
    {
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"),
            await CreateProductAsync(10m), quantity: 1, 10m);
        var tag = PostgresApiFixture.Unique("Tag");
        var title = PostgresApiFixture.Unique("Renamed");

        var response = await client.PutAsJsonAsync($"{path}/{id}/details",
            new { title, tags = new[] { tag }, occurrenceTime = OccurredAt });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.Equal(tag, body.GetProperty("tags")[0].GetString());
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task UpdateEntries_ChangesAddsAndRemovesEntries(string path)
    {
        var first = await CreateProductAsync(10m);
        var second = await CreateProductAsync(4m);
        var third = await CreateProductAsync(1m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), first, quantity: 1, 10m);
        var entryId = await FirstEntryIdAsync(path, id);

        var changed = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new object[]
            {
                new { id = entryId, productId = first, quantity = 2, unitCost = 10m },
                new { id = (string?)null, productId = second, quantity = 1, unitCost = 4m },
            },
        });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(24m, Total(path, await changed.ReadJsonAsync()));

        var replaced = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new object[]
            {
                new { id = (string?)null, productId = third, quantity = 5, unitCost = 1m },
            },
        });
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);

        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        Assert.Equal(1, body.GetProperty("productEntries").GetArrayLength());
        Assert.Equal(5m, Total(path, body));
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task UpdateEntries_WithNewEntryForKeptProduct_AddsToThatEntry(string path)
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1, 10m);
        var entryId = await FirstEntryIdAsync(path, id);

        var changed = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new object[]
            {
                new { id = entryId, productId = product, quantity = 3, unitCost = 10m },
                new { id = (string?)null, productId = product, quantity = 2, unitCost = 10m },
            },
        });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        var entries = body.GetProperty("productEntries");
        Assert.Equal(1, entries.GetArrayLength());
        Assert.Equal(entryId, entries[0].GetProperty("id").GetString());
        Assert.Equal(50m, Total(path, body));
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task UpdateEntries_WithForeignEntryOrUnknownProduct_ReturnsNotFound(string path)
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1, 10m);

        var foreign = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new[] { new { id = Guid.NewGuid(), productId = product, quantity = 1, unitCost = 1m } },
        });
        var unknown = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new[] { new { productId = Guid.NewGuid(), quantity = 1, unitCost = 1m } },
        });

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Create_WithUnknownProduct_ReturnsNotFound(string path)
    {
        var response = await client.PostAsJsonAsync(path, new
        {
            title = "Title",
            tags = Array.Empty<string>(),
            productEntries = new[] { new { id = Guid.NewGuid(), quantity = 1, unitCost = 1m } },
            occurrenceTime = OccurredAt,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Delete_RemovesTransactionAndFreesTheProduct(string path)
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1, 10m);

        var blocked = await client.DeleteAsync($"/products/{product}");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("Products.InUse", (await blocked.ReadJsonAsync()).GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/products/{product}")).StatusCode);
    }

    [Fact]
    public async Task DeletingClosedPriceUsedByASaleEntry_ReturnsConflict()
    {
        var product = await CreateProductAsync(10m);
        await CreateTransactionAsync("/sales", PostgresApiFixture.Unique("Title"), product, quantity: 1, 10m);
        var previousPriceId = await ChangePriceAsync(product, 20m);

        var response = await client.DeleteAsync($"/products/prices/{previousPriceId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Products.PriceInUse", (await response.ReadJsonAsync()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task DeletingClosedPrice_IsNotBlockedByPurchases()
    {
        var product = await CreateProductAsync(10m);
        await CreateTransactionAsync(Purchases, PostgresApiFixture.Unique("Title"), product, quantity: 1, 7m);
        var previousPriceId = await ChangePriceAsync(product, 20m);

        var response = await client.DeleteAsync($"/products/prices/{previousPriceId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<int> CountAsync(string path, string query)
    {
        var response = await client.GetAsync($"{path}?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("totalCount").GetInt32();
    }

    private async Task<string> CreateTransactionAsync(string path, string title, string productId, int quantity,
        decimal unitCost)
    {
        var response = await client.PostAsJsonAsync(path, new
        {
            title,
            tags = new[] { "Tag" },
            productEntries = new[] { new { id = productId, quantity, unitCost } },
            occurrenceTime = OccurredAt,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response.CreatedId();
    }

    private async Task<string?> FirstEntryIdAsync(string path, string id)
    {
        return (await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync())
            .GetProperty("productEntries")[0].GetProperty("id").GetString();
    }

    private async Task<string?> ChangePriceAsync(string productId, decimal value)
    {
        var price = await client.PostAsJsonAsync($"/products/{productId}/prices", new { value });
        return (await price.ReadJsonAsync()).GetProperty("previousPriceId").GetString();
    }

    private async Task<string> CreateProductAsync(decimal price)
    {
        var brand = await client.PostAsJsonAsync("/brands", new { name = PostgresApiFixture.Unique("Brand") });
        var unit = await client.PostAsJsonAsync("/products/units", new { name = PostgresApiFixture.Unique("Unit") });
        var product = await client.PostAsJsonAsync("/products", new
        {
            name = PostgresApiFixture.Unique("Product"),
            brandId = int.Parse(brand.CreatedId()),
            measure = 1f,
            measureUnitId = int.Parse(unit.CreatedId()),
            price,
            tags = Array.Empty<string>(),
        });

        Assert.Equal(HttpStatusCode.Created, product.StatusCode);
        return (await product.ReadJsonAsync()).GetProperty("id").GetString()!;
    }

    private static decimal Total(string path, JsonElement body)
    {
        return body.GetProperty(path == Purchases ? "totalCost" : "totalPrice").GetDecimal();
    }
}
