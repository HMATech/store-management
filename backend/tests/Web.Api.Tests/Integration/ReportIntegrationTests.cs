using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Web.Api.Tests.Integration;

/// <summary>Uses dates in 2031 that no other test writes to, so the totals are exact.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class ReportIntegrationTests(PostgresApiFixture api)
{
    private readonly HttpClient client = api.CreateClient();

    [Fact]
    public async Task DailyTotals_GroupsTransactionsByLocalDayInSql()
    {
        var product = await CreateProductAsync(10m);
        // 01:00 UTC is 08:00 on the 5th in Jakarta; 20:00 UTC is 03:00 on the 6th.
        await CreateAsync("/purchases", "2031-03-05T01:00:00Z", new { id = product, quantity = 4, unitCost = 6m });
        await CreateAsync("/sales", "2031-03-05T20:00:00Z", new { id = product, quantity = 2, unitCost = 0m });
        await CreateAsync("/sales", "2031-03-06T02:00:00Z");

        var body = await GetDailyTotalsAsync("from=2031-03-05&to=2031-03-06&timeZone=Asia/Jakarta");

        var days = body.GetProperty("days");
        Assert.Equal(2, days.GetArrayLength());
        AssertDay(days[0], "2031-03-05", salesTotal: 0m, salesCount: 0, purchasesTotal: 24m, purchasesCount: 1);
        AssertDay(days[1], "2031-03-06", salesTotal: 20m, salesCount: 2, purchasesTotal: 0m, purchasesCount: 0);
        Assert.Equal(20m, body.GetProperty("salesTotal").GetDecimal());
        Assert.Equal(24m, body.GetProperty("purchasesTotal").GetDecimal());
    }

    [Fact]
    public async Task DailyTotals_WithoutTimeZone_UsesUtcDays()
    {
        var product = await CreateProductAsync(5m);
        await CreateAsync("/sales", "2031-04-10T23:30:00Z", new { id = product, quantity = 1, unitCost = 0m });

        var body = await GetDailyTotalsAsync("from=2031-04-10&to=2031-04-11");

        Assert.Equal("UTC", body.GetProperty("timeZone").GetString());
        Assert.Equal(1, body.GetProperty("days")[0].GetProperty("salesCount").GetInt32());
        Assert.Equal(0, body.GetProperty("days")[1].GetProperty("salesCount").GetInt32());
    }

    [Fact]
    public async Task DailyTotals_WithoutToken_ReturnsUnauthorized()
    {
        var response = await api.CreateAnonymousClient()
            .GetAsync("/reports/daily-totals?from=2031-01-01&to=2031-01-01");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<JsonElement> GetDailyTotalsAsync(string query)
    {
        var response = await client.GetAsync($"/reports/daily-totals?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    private async Task CreateAsync(string path, string occurredAt, params object[] entries)
    {
        var response = await client.PostAsJsonAsync(path, new
        {
            title = PostgresApiFixture.Unique("Report"),
            tags = Array.Empty<string>(),
            productEntries = entries,
            occurrenceTime = occurredAt,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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

    private static void AssertDay(JsonElement day, string date, decimal salesTotal, int salesCount,
        decimal purchasesTotal, int purchasesCount)
    {
        Assert.Equal(date, day.GetProperty("date").GetString());
        Assert.Equal(salesTotal, day.GetProperty("salesTotal").GetDecimal());
        Assert.Equal(salesCount, day.GetProperty("salesCount").GetInt32());
        Assert.Equal(purchasesTotal, day.GetProperty("purchasesTotal").GetDecimal());
        Assert.Equal(purchasesCount, day.GetProperty("purchasesCount").GetInt32());
    }
}
