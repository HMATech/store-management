using Application.Common;
using Application.Tests.TestSupport;
using Application.Transactions;
using Application.Transactions.Get;
using Domain.Purchases;
using Domain.Sales;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Tests.Transactions;

public class TransactionHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task Get_ReturnsSalesAndPurchasesTogether()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var page = await GetAsync(new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC"));

        Assert.Equal(2, page.TotalCount);
        var sale = page.Responses.Single(t => t.Kind == TransactionKind.Sale);
        var purchase = page.Responses.Single(t => t.Kind == TransactionKind.Purchase);
        Assert.Equal(new TransactionSummaryResponse(graph.Sale.Id, TransactionKind.Sale, "Morning sale",
            SeededGraph.OccurredAt, LineCount: 1, Quantity: 3, Total: 30m), sale);
        Assert.Equal(new TransactionSummaryResponse(graph.Purchase.Id, TransactionKind.Purchase, "Weekly restock",
            SeededGraph.OccurredAt, LineCount: 1, Quantity: 3, Total: 18m), purchase);
    }

    [Fact]
    public async Task Get_OrdersNewestFirstAndOldestFirst()
    {
        await SeededGraph.CreateAsync(database);
        await database.SeedAsync(Purchase.CreateNew("Later", SeededGraph.OccurredAt.AddHours(2), FixedClock.Now),
            Sale.CreateNew("Earlier", SeededGraph.OccurredAt.AddHours(-2), FixedClock.Now));

        var descending = await GetAsync(new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC"));
        var ascending = await GetAsync(new GetTransactionQuery(SortOrder.ASC, null, null, null, "UTC"));

        Assert.Equal("Later", descending.Responses[0].Title);
        Assert.Equal("Earlier", descending.Responses[^1].Title);
        Assert.Equal("Earlier", ascending.Responses[0].Title);
    }

    [Theory]
    [InlineData("sale", TransactionKind.Sale)]
    [InlineData("PURCHASE", TransactionKind.Purchase)]
    public async Task Get_FiltersByKindIgnoringCase(string kind, TransactionKind expected)
    {
        await SeededGraph.CreateAsync(database);

        var page = await GetAsync(new GetTransactionQuery(SortOrder.DESC, kind, null, null, "UTC"));

        Assert.Equal(expected, Assert.Single(page.Responses).Kind);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Get_FiltersByLocalDaysOfTheTimeZone()
    {
        await SeededGraph.CreateAsync(database);
        // 20:00 UTC on the 10th is the 11th in Jakarta.
        await database.SeedAsync(Sale.CreateNew("Late", new DateTime(2026, 1, 10, 20, 0, 0, DateTimeKind.Utc),
            FixedClock.Now));
        var eleventh = new DateOnly(2026, 1, 11);

        var utc = await GetAsync(new GetTransactionQuery(SortOrder.DESC, null, eleventh, eleventh, "UTC"));
        var jakarta = await GetAsync(new GetTransactionQuery(SortOrder.DESC, null, eleventh, eleventh,
            "Asia/Jakarta"));

        Assert.Empty(utc.Responses);
        Assert.Equal("Late", Assert.Single(jakarta.Responses).Title);
    }

    [Fact]
    public async Task Get_WithOnlyAStartDay_KeepsEverythingFromThen()
    {
        await SeededGraph.CreateAsync(database);
        await database.SeedAsync(Sale.CreateNew("Next day", SeededGraph.OccurredAt.AddDays(1), FixedClock.Now));

        var page = await GetAsync(new GetTransactionQuery(SortOrder.DESC, null,
            DateOnly.FromDateTime(SeededGraph.OccurredAt.AddDays(1)), null, "UTC"));

        Assert.Equal("Next day", Assert.Single(page.Responses).Title);
    }

    [Fact]
    public async Task Get_PagesAcrossSalesAndPurchases()
    {
        await SeededGraph.CreateAsync(database);
        await database.SeedAsync(Purchase.CreateNew("Later", SeededGraph.OccurredAt.AddHours(2), FixedClock.Now));

        var first = await GetAsync(new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC", Page: 1,
            PageSize: 2));
        var second = await GetAsync(new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC", Page: 2,
            PageSize: 2));

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.Responses.Count);
        Assert.True(first.HasNextPage);
        Assert.Single(second.Responses);
        Assert.Empty(first.Responses.Select(r => r.Id).Intersect(second.Responses.Select(r => r.Id)));
    }

    [Fact]
    public async Task Get_CountsATransactionWithoutEntriesAtZero()
    {
        await database.SeedAsync(Sale.CreateNew("Empty", SeededGraph.OccurredAt, FixedClock.Now));

        var transaction = Assert.Single((await GetAsync(
            new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC"))).Responses);

        Assert.Equal(0, transaction.LineCount);
        Assert.Equal(0, transaction.Quantity);
        Assert.Equal(0m, transaction.Total);
    }

    [Theory]
    [InlineData("sale", true, TransactionKind.Sale)]
    [InlineData("Purchase", true, TransactionKind.Purchase)]
    [InlineData("0", false, TransactionKind.Sale)]
    [InlineData("refund", false, TransactionKind.Sale)]
    [InlineData(null, false, TransactionKind.Sale)]
    public void TryParseKind_AcceptsOnlyNames(string? value, bool expected, TransactionKind kind)
    {
        Assert.Equal(expected, GetTransactionQueryHandler.TryParseKind(value, out var parsed));
        Assert.Equal(kind, parsed);
    }

    [Fact]
    public void Query_ToString_IncludesOnlyProvidedFilters()
    {
        var text = new GetTransactionQuery(SortOrder.DESC, "sale", null, new DateOnly(2026, 1, 2), "UTC").ToString();

        Assert.Contains("Kind: sale", text);
        Assert.Contains("To:", text);
        Assert.DoesNotContain("From:", text);
    }

    private async Task<PagedResponse<TransactionSummaryResponse>> GetAsync(GetTransactionQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetTransactionQueryHandler(context, NullLogger<GetTransactionQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }
}
