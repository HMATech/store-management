using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Web.Api.Tests.Integration;

/// <summary>Uses dates in 2032 that no other test writes to, so the history holds only these transactions.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class TransactionHistoryIntegrationTests(PostgresApiFixture api)
{
    // May 1 in Jakarta ends at 17:00 UTC, so it holds the first test's rows and none of the second's.
    private const string Range = "from=2032-05-01&to=2032-05-01&timeZone=Asia/Jakarta";

    private readonly HttpClient client = api.CreateClient();

    [Fact]
    public async Task History_CombinesSalesAndPurchasesInSql()
    {
        var product = await CreateProductAsync(10m);
        var purchaseId = await CreateAsync("/purchases", "Restock", "2032-05-01T01:00:00Z",
            new { id = product, quantity = 4, unitCost = 6m });
        var saleId = await CreateAsync("/sales", "", "2032-05-01T03:00:00Z",
            new { id = product, quantity = 2, unitCost = 0m });
        await CreateAsync("/sales", "Empty", "2032-05-01T05:00:00Z");

        var body = await GetAsync(Range);

        Assert.Equal(3, body.GetProperty("totalCount").GetInt32());
        var rows = body.GetProperty("responses");
        Assert.Equal(["Sale", "Sale", "Purchase"],
            rows.EnumerateArray().Select(r => r.GetProperty("kind").GetString()));
        AssertRow(rows[1], saleId, "Sale", lineCount: 1, quantity: 2, total: 20m);
        AssertRow(rows[2], purchaseId, "Purchase", lineCount: 1, quantity: 4, total: 24m);
        Assert.Equal(0m, rows[0].GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task History_FiltersByKindAndPagesInSql()
    {
        var product = await CreateProductAsync(5m);
        for (var hour = 1; hour <= 3; hour++)
        {
            await CreateAsync("/sales", $"S{hour}", $"2032-05-02T0{hour}:00:00Z",
                new { id = product, quantity = 1, unitCost = 0m });
        }
        await CreateAsync("/purchases", "P", "2032-05-02T04:00:00Z",
            new { id = product, quantity = 1, unitCost = 1m });

        var salesOnly = await GetAsync("from=2032-05-02&to=2032-05-02&kind=sale&pageSize=2");
        var oldestFirst = await GetAsync("from=2032-05-02&to=2032-05-02&sortOrder=ASC&page=2&pageSize=3");

        Assert.Equal(3, salesOnly.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, salesOnly.GetProperty("responses").GetArrayLength());
        Assert.True(salesOnly.GetProperty("hasNextPage").GetBoolean());
        Assert.Equal("P", oldestFirst.GetProperty("responses")[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task History_WithoutToken_ReturnsUnauthorized()
    {
        var response = await api.CreateAnonymousClient().GetAsync("/transactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<JsonElement> GetAsync(string query)
    {
        var response = await client.GetAsync($"/transactions?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    private async Task<string> CreateAsync(string path, string title, string occurredAt, params object[] entries)
    {
        var response = await client.PostAsJsonAsync(path, new
        {
            title,
            tags = Array.Empty<string>(),
            productEntries = entries,
            occurrenceTime = occurredAt,
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

    private static void AssertRow(JsonElement row, string id, string kind, int lineCount, long quantity,
        decimal total)
    {
        Assert.Equal(id, row.GetProperty("id").GetString());
        Assert.Equal(kind, row.GetProperty("kind").GetString());
        Assert.Equal(lineCount, row.GetProperty("lineCount").GetInt32());
        Assert.Equal(quantity, row.GetProperty("quantity").GetInt64());
        Assert.Equal(total, row.GetProperty("total").GetDecimal());
    }
}
