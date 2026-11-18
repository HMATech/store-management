using Application.Reports.DailyTotals;
using Application.Tests.TestSupport;
using Domain.Purchases;
using Domain.Sales;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Reports;

public class DailyTotalsHandlerTests
{
    private const string Jakarta = "Asia/Jakarta";

    private static readonly DateOnly SeedDay = DateOnly.FromDateTime(SeededGraph.OccurredAt);

    private readonly TestDatabase database = new();

    [Fact]
    public async Task Get_ReturnsEveryDayInTheRangeIncludingEmptyOnes()
    {
        await SeededGraph.CreateAsync(database);

        var response = await GetAsync(SeedDay.AddDays(-1), SeedDay.AddDays(1), "UTC");

        Assert.Equal([SeedDay.AddDays(-1), SeedDay, SeedDay.AddDays(1)], response.Days.Select(d => d.Date));
        Assert.Equal(new DayTotalsResponse(SeedDay.AddDays(-1), 0m, 0, 0m, 0, 0m, 0m, 0), response.Days[0]);
        Assert.Equal(new DayTotalsResponse(SeedDay.AddDays(1), 0m, 0, 0m, 0, 0m, 0m, 0), response.Days[2]);
    }

    [Fact]
    public async Task Get_ValuesSalesAtSellingPriceAndPurchasesAtCost()
    {
        await SeededGraph.CreateAsync(database);

        var day = Assert.Single((await GetAsync(SeedDay, SeedDay, "UTC")).Days);

        // The seed buys 3 at a cost of 6 and sells them at the price of 10, a profit of 4 each.
        Assert.Equal(new DayTotalsResponse(SeedDay, 30m, 1, 18m, 1, 18m, 12m, 0), day);
    }

    [Fact]
    public async Task Get_SumsTheRangeTotals()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await AddSaleAsync(graph, SeededGraph.OccurredAt.AddDays(1), quantity: 2);

        var response = await GetAsync(SeedDay, SeedDay.AddDays(1), "UTC");

        Assert.Equal(50m, response.SalesTotal);
        Assert.Equal(2, response.SalesCount);
        Assert.Equal(18m, response.PurchasesTotal);
        Assert.Equal(1, response.PurchasesCount);
        Assert.Equal(SeedDay, response.From);
        Assert.Equal(SeedDay.AddDays(1), response.To);
        Assert.Equal("UTC", response.TimeZone);
    }

    [Fact]
    public async Task Get_GroupsByTheLocalDayOfTheTimeZone()
    {
        var graph = await SeededGraph.CreateAsync(database);
        // 20:00 UTC on the 10th is 03:00 on the 11th in Jakarta (UTC+7).
        await AddSaleAsync(graph, new DateTime(2026, 1, 10, 20, 0, 0, DateTimeKind.Utc), quantity: 1);

        var utc = await GetAsync(SeedDay, SeedDay.AddDays(1), "UTC");
        var jakarta = await GetAsync(SeedDay, SeedDay.AddDays(1), Jakarta);

        Assert.Equal([2, 0], utc.Days.Select(d => d.SalesCount));
        Assert.Equal([1, 1], jakarta.Days.Select(d => d.SalesCount));
        Assert.Equal(10m, jakarta.Days[1].SalesTotal);
    }

    [Fact]
    public async Task Get_IncludesTheFirstMomentAndExcludesTheNextDay()
    {
        var graph = await SeededGraph.CreateAsync(database);
        // Midnight on the 12th in Jakarta is 17:00 UTC on the 11th.
        await AddSaleAsync(graph, new DateTime(2026, 1, 11, 17, 0, 0, DateTimeKind.Utc), quantity: 1);
        await AddSaleAsync(graph, new DateTime(2026, 1, 12, 17, 0, 0, DateTimeKind.Utc), quantity: 1);

        var response = await GetAsync(new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 12), Jakarta);

        Assert.Equal(1, Assert.Single(response.Days).SalesCount);
    }

    [Fact]
    public async Task Get_CountsASaleWithoutEntriesAtZero()
    {
        await SeededGraph.CreateAsync(database);
        await database.SeedAsync(Sale.CreateNew("Empty", SeededGraph.OccurredAt, FixedClock.Now));

        var day = Assert.Single((await GetAsync(SeedDay, SeedDay, "UTC")).Days);

        Assert.Equal(2, day.SalesCount);
        Assert.Equal(30m, day.SalesTotal);
    }

    [Fact]
    public async Task Get_SumsPurchaseLinesAtTheirOwnCosts()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var purchase = Purchase.CreateNew("Two costs", SeededGraph.OccurredAt, FixedClock.Now);
        purchase.UpdateProductEntries(
        [
            PurchaseProductEntry.CreateNew(purchase.Id, graph.Product.Id, 6m, 2),
            PurchaseProductEntry.CreateNew(purchase.Id, graph.Product.Id, 5.5m, 4),
        ]);
        await database.SeedAsync(purchase);

        var day = Assert.Single((await GetAsync(SeedDay, SeedDay, "UTC")).Days);

        Assert.Equal(2, day.PurchasesCount);
        Assert.Equal(18m + 12m + 22m, day.PurchasesTotal);
    }

    [Fact]
    public async Task Get_CostsSalesAtTheMovingAverageOfWhatWasPaid()
    {
        var graph = await SeededGraph.CreateAsync(database);
        // The seed buys 3, sells 3 and spills 1, so the shelf is 1 short. 5 at 9 set the average to 9 and leave 4,
        // and 2 more at 6 bring it to (4 x 9 + 2 x 6) / 6 = 8.
        await AddPurchaseAsync(graph, SeededGraph.OccurredAt.AddDays(1), unitCost: 9m, quantity: 5);
        await AddPurchaseAsync(graph, SeededGraph.OccurredAt.AddDays(2), unitCost: 6m, quantity: 2);
        await AddSaleAsync(graph, SeededGraph.OccurredAt.AddDays(3), quantity: 3);

        var day = (await GetAsync(SeedDay.AddDays(3), SeedDay.AddDays(3), "UTC")).Days.Single();

        Assert.Equal(24m, day.CostOfSales);
        Assert.Equal(30m - 24m, day.GrossProfit);
        Assert.Equal(0, day.UncostedQuantity);
    }

    [Fact]
    public async Task Get_KeepsAnEarlierSalesCostWhenGoodsAreBoughtLater()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await AddPurchaseAsync(graph, SeededGraph.OccurredAt.AddDays(1), unitCost: 9m, quantity: 10);

        var day = (await GetAsync(SeedDay, SeedDay.AddDays(1), "UTC")).Days[0];

        Assert.Equal(18m, day.CostOfSales);
    }

    [Fact]
    public async Task Get_LeavesOutOfTheProfitWhatWasSoldBeforeAnyPurchase()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await AddSaleAsync(graph, SeededGraph.OccurredAt.AddDays(-1), quantity: 2);

        var response = await GetAsync(SeedDay.AddDays(-1), SeedDay, "UTC");

        Assert.Equal(new DayTotalsResponse(SeedDay.AddDays(-1), 20m, 1, 0m, 0, 0m, 0m, 2), response.Days[0]);
        Assert.Equal(12m, response.GrossProfit);
        Assert.Equal(18m, response.CostOfSales);
        Assert.Equal(2, response.UncostedQuantity);
    }

    [Fact]
    public async Task Get_RoundsAnAverageThatDoesNotDivideEvenly()
    {
        var graph = await SeededGraph.CreateAsync(database);
        // The shelf is 1 short after the seed, so 2 at 1 leave 1 at 1. 2 more at 2 make an average of 5 / 3.
        await AddPurchaseAsync(graph, SeededGraph.OccurredAt.AddDays(1), unitCost: 1m, quantity: 2);
        await AddPurchaseAsync(graph, SeededGraph.OccurredAt.AddDays(1).AddHours(1), unitCost: 2m, quantity: 2);
        await AddSaleAsync(graph, SeededGraph.OccurredAt.AddDays(2), quantity: 1);

        var day = (await GetAsync(SeedDay.AddDays(2), SeedDay.AddDays(2), "UTC")).Days.Single();

        Assert.Equal(1.67m, day.CostOfSales);
        Assert.Equal(8.33m, day.GrossProfit);
    }

    [Fact]
    public async Task Get_WithNoTransactions_ReturnsZeroTotals()
    {
        var response = await GetAsync(new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 7), Jakarta);

        Assert.Equal(7, response.Days.Length);
        Assert.All(response.Days, d => Assert.Equal(0, d.SalesCount + d.PurchasesCount));
        Assert.Equal(0m, response.SalesTotal + response.PurchasesTotal);
    }

    [Fact]
    public void Query_ToString_ListsTheRangeAndTimeZone()
    {
        var from = new DateOnly(2026, 1, 2);

        var text = new GetDailyTotalsQuery(from, new DateOnly(2026, 1, 3), Jakarta).ToString();

        Assert.Contains($"From: {from}", text);
        Assert.Contains("TimeZone: Asia/Jakarta", text);
    }

    private async Task AddSaleAsync(SeededGraph graph, DateTime occurredAt, int quantity)
    {
        var sale = Sale.CreateNew("Extra", occurredAt, FixedClock.Now);
        sale.UpdateProductEntries(
            [SaleProductEntry.CreateNew(sale.Id, graph.Product.Id, graph.CurrentPrice.Id, quantity)]);
        await database.SeedAsync(sale);
    }

    private async Task AddPurchaseAsync(SeededGraph graph, DateTime occurredAt, decimal unitCost, int quantity)
    {
        var purchase = Purchase.CreateNew("Restock", occurredAt, FixedClock.Now);
        purchase.UpdateProductEntries(
            [PurchaseProductEntry.CreateNew(purchase.Id, graph.Product.Id, unitCost, quantity)]);
        await database.SeedAsync(purchase);
    }

    private async Task<DailyTotalsResponse> GetAsync(DateOnly from, DateOnly to, string timeZone)
    {
        await using var context = database.CreateContext();
        var handler = new GetDailyTotalsQueryHandler(context, NullLogger<GetDailyTotalsQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(new GetDailyTotalsQuery(from, to, timeZone), default));
    }
}
