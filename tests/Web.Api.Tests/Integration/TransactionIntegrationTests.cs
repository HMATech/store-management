using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Web.Api.Tests.Integration;

/// <summary>Purchases and sales share a contract, so each test runs against both.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class TransactionIntegrationTests(PostgresApiFixture api)
{
    private const string OccurredAt = "2026-01-10T12:00:00Z";

    private readonly HttpClient client = api.CreateClient();

    public static TheoryData<string> Resources => new() { "/purchases", "/sales" };

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Create_ThenGetById_ReturnsEntriesAtCurrentPrice(string path)
    {
        var product = await CreateProductAsync(10m);

        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 3);

        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        Assert.Equal(30m, body.GetProperty("totalPrice").GetDecimal());
        var entry = body.GetProperty("productEntries")[0];
        Assert.Equal(product, entry.GetProperty("productId").GetString());
        Assert.Equal(10m, entry.GetProperty("priceValue").GetDecimal());
        Assert.Equal(3, entry.GetProperty("quantity").GetInt32());
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Entries_KeepThePriceFromWhenTheyWereCreated(string path)
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1);

        await client.PostAsJsonAsync($"/products/{product}/prices", new { value = 99m });

        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        Assert.Equal(10m, body.GetProperty("totalPrice").GetDecimal());
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Get_FiltersBySearchDateAndTotalInSql(string path)
    {
        var title = PostgresApiFixture.Unique("Title");
        var product = await CreateProductAsync(10m);
        await CreateTransactionAsync(path, title, product, quantity: 3);

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
            await CreateProductAsync(10m), quantity: 1);
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
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), first, quantity: 1);
        var entryId = (await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync())
            .GetProperty("productEntries")[0].GetProperty("id").GetString();

        var changed = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new object[]
            {
                new { id = entryId, productId = first, quantity = 2 },
                new { id = (string?)null, productId = second, quantity = 1 },
            },
        });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        Assert.Equal(24m, (await changed.ReadJsonAsync()).GetProperty("totalPrice").GetDecimal());

        var replaced = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new object[] { new { id = (string?)null, productId = third, quantity = 5 } },
        });
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);

        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        Assert.Equal(1, body.GetProperty("productEntries").GetArrayLength());
        Assert.Equal(5m, body.GetProperty("totalPrice").GetDecimal());
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task UpdateEntries_WithNewEntryForKeptProduct_AddsToThatEntry(string path)
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1);
        var entryId = (await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync())
            .GetProperty("productEntries")[0].GetProperty("id").GetString();

        var changed = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new object[]
            {
                new { id = entryId, productId = product, quantity = 3 },
                new { id = (string?)null, productId = product, quantity = 2 },
            },
        });
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        var body = await (await client.GetAsync($"{path}/{id}")).ReadJsonAsync();
        var entries = body.GetProperty("productEntries");
        Assert.Equal(1, entries.GetArrayLength());
        Assert.Equal(entryId, entries[0].GetProperty("id").GetString());
        Assert.Equal(50m, body.GetProperty("totalPrice").GetDecimal());
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task UpdateEntries_WithForeignEntryOrUnknownProduct_ReturnsNotFound(string path)
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1);

        var foreign = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new[] { new { id = Guid.NewGuid(), productId = product, quantity = 1 } },
        });
        var unknown = await client.PutAsJsonAsync($"{path}/{id}/entries", new
        {
            productEntries = new[] { new { productId = Guid.NewGuid(), quantity = 1 } },
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
            productEntries = new[] { new { id = Guid.NewGuid(), quantity = 1 } },
            occurrenceTime = OccurredAt,
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task Delete_RemovesTransactionAndFreesTheProduct(string path)
    {
        var product = await CreateProductAsync(10m);
        var id = await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1);

        var blocked = await client.DeleteAsync($"/products/{product}");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("Products.InUse", (await blocked.ReadJsonAsync()).GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{path}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/products/{product}")).StatusCode);
    }

    [Theory]
    [MemberData(nameof(Resources))]
    public async Task DeletingClosedPriceUsedByAnEntry_ReturnsConflict(string path)
    {
        var product = await CreateProductAsync(10m);
        await CreateTransactionAsync(path, PostgresApiFixture.Unique("Title"), product, quantity: 1);
        var price = await client.PostAsJsonAsync($"/products/{product}/prices", new { value = 20m });
        var previousPriceId = (await price.ReadJsonAsync()).GetProperty("previousPriceId").GetString();

        var response = await client.DeleteAsync($"/products/prices/{previousPriceId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Products.PriceInUse", (await response.ReadJsonAsync()).GetProperty("title").GetString());
    }

    private async Task<int> CountAsync(string path, string query)
    {
        var response = await client.GetAsync($"{path}?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("totalCount").GetInt32();
    }

    private async Task<string> CreateTransactionAsync(string path, string title, string productId, int quantity)
    {
        var response = await client.PostAsJsonAsync(path, new
        {
            title,
            tags = new[] { "Tag" },
            productEntries = new[] { new { id = productId, quantity } },
            occurrenceTime = OccurredAt,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response.CreatedId();
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
}
