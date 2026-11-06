using Application.Common;
using Application.Purchases;
using Application.Purchases.Create;
using Application.Purchases.DeleteById;
using Application.Purchases.Get;
using Application.Purchases.GetById;
using Application.Purchases.UpdateDetailsById;
using Application.Purchases.UpdateEntriesById;
using Application.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using CreateEntry = Application.Purchases.Create.ProductEntryCommand;
using UpdateEntry = Application.Purchases.UpdateEntriesById.ProductEntryCommand;

namespace Application.Tests.Purchases;

public class PurchaseHandlerTests
{
    private static readonly DateTime OccurredAt = new(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase database = new();

    [Fact]
    public async Task Create_WithValidInput_UsesCurrentPriceOfEachProduct()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await CreateAsync(new CreatePurchaseCommand("New", ["Stock", "Urgent"],
            [new CreateEntry(graph.Product.Id, 2), new CreateEntry(graph.OtherProduct.Id, 1)], OccurredAt));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(FixedClock.Now, response.CreatedTime);

        var purchase = await database.ReadAsync(c => c.Purchases
            .Include(p => p.Products)
            .Include(p => p.Tags)
            .SingleAsync(p => p.Id == response.Id));
        Assert.Equal("New", purchase.Title);
        Assert.Equal(OccurredAt, purchase.OccurenceTime);
        Assert.Equal(["stock", "urgent"], purchase.Tags.Select(t => t.Name.Normalized).Order());
        Assert.Equal(2, purchase.Products.Count);
        var entry = purchase.Products.Single(e => e.ProductId == graph.Product.Id);
        Assert.Equal(graph.CurrentPrice.Id, entry.PriceId);
        Assert.Equal(2, entry.Quantity);
    }

    [Fact]
    public async Task Create_ReusesExistingTagInsteadOfDuplicatingIt()
    {
        var graph = await SeededGraph.CreateAsync(database);

        ResultAssert.Succeeded(await CreateAsync(new CreatePurchaseCommand("New", ["STOCK"],
            [new CreateEntry(graph.Product.Id, 1)], OccurredAt)));

        Assert.Equal(1, await database.ReadAsync(c => c.PurchaseTags.CountAsync()));
    }

    [Fact]
    public async Task Create_WithoutTags_Succeeds()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var response = ResultAssert.Succeeded(await CreateAsync(new CreatePurchaseCommand("New", [],
            [new CreateEntry(graph.Product.Id, 1)], OccurredAt)));

        var purchase = await database.ReadAsync(c => c.Purchases
            .Include(p => p.Tags)
            .SingleAsync(p => p.Id == response.Id));
        Assert.Empty(purchase.Tags);
    }

    [Fact]
    public async Task Create_WithUnknownProduct_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var unknownId = Guid.NewGuid();

        var result = await CreateAsync(new CreatePurchaseCommand("New", [],
            [new CreateEntry(graph.Product.Id, 1), new CreateEntry(unknownId, 1)], OccurredAt));

        ResultAssert.Failed(result, "Purchase.NonExistingProduct(s)", ErrorType.NotFound);
        Assert.Contains(unknownId.ToString(), result.Error.Description);
    }

    [Fact]
    public async Task Create_WithSameProductTwice_KeepsTotalQuantity()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var response = ResultAssert.Succeeded(await CreateAsync(new CreatePurchaseCommand("New", [],
            [new CreateEntry(graph.Product.Id, 2), new CreateEntry(graph.Product.Id, 3)], OccurredAt)));

        var entries = await database.ReadAsync(c => c.PurchaseProductEntries
            .Where(e => e.PurchaseId == response.Id)
            .ToListAsync());
        Assert.Equal(5, entries.Sum(e => e.Quantity));
    }

    [Fact]
    public async Task DeleteById_WithExistingPurchase_RemovesIt()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new DeletePurchaseByIdCommandHandler(context,
            NullLogger<DeletePurchaseByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeletePurchaseByIdCommand(graph.Purchase.Id), default));

        Assert.False(await database.ReadAsync(c => c.Purchases.AnyAsync()));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeletePurchaseByIdCommandHandler(context,
            NullLogger<DeletePurchaseByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeletePurchaseByIdCommand(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Purchase.NotFound", ErrorType.NotFound);
    }

    [Theory]
    [InlineData("RESTOCK", 1)]
    [InlineData("missing", 0)]
    [InlineData(null, 1)]
    public async Task Get_SearchesTitleIgnoringCase(string? search, int expected)
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(Query(search: search));

        Assert.Equal(expected, value.TotalCount);
    }

    [Theory]
    [InlineData("2026-01-10", "2026-01-10", 1)]
    [InlineData("2026-01-11", null, 0)]
    [InlineData(null, "2026-01-09", 0)]
    public async Task Get_FiltersByOccurrenceDates(string? from, string? to, int expected)
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(Query(
            from: from == null ? null : DateOnly.Parse(from),
            to: to == null ? null : DateOnly.Parse(to)));

        Assert.Equal(expected, value.TotalCount);
    }

    [Theory]
    [InlineData(30, 30, 1)]
    [InlineData(31, null, 0)]
    [InlineData(null, 29, 0)]
    public async Task Get_FiltersByTotal(int? from, int? to, int expected)
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(Query(totalFrom: from, totalTo: to));

        Assert.Equal(expected, value.TotalCount);
    }

    [Fact]
    public async Task Get_SortsByOccurrenceTime()
    {
        await SeededGraph.CreateAsync(database);
        var later = Domain.Purchases.Purchase.CreateNew("Later", OccurredAt, FixedClock.Now);
        await database.SeedAsync(later);

        var ascending = await GetAsync(Query());
        var descending = await GetAsync(Query(order: SortOrder.DESC));

        Assert.Equal(["Weekly restock", "Later"], ascending.Responses.Select(p => p.Title));
        Assert.Equal(["Later", "Weekly restock"], descending.Responses.Select(p => p.Title));
    }

    [Fact]
    public async Task GetById_ReturnsPurchaseWithEntriesAndTotals()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new GetPurchaseByIdQueryHandler(context, NullLogger<GetPurchaseByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(
            await handler.HandleAsync(new GetPurchaseByIdQuery(graph.Purchase.Id), default));

        Assert.Equal("Weekly restock", response.Title);
        Assert.Equal(["Stock"], response.Tags);
        Assert.Equal(30m, response.TotalPrice);
        Assert.Equal(SeededGraph.OccurredAt, response.OccurrenceTime);
        Assert.Equal(new ProductEntryResponse(graph.PurchaseEntry.Id, graph.Product.Id, "Milk",
            graph.CurrentPrice.Id, 10m, 3, 30m), Assert.Single(response.ProductEntries));
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetPurchaseByIdQueryHandler(context, NullLogger<GetPurchaseByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetPurchaseByIdQuery(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Purchase.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateDetailsById_ChangesTitleTagsAndOccurrence()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateDetailsAsync(new UpdatePurchaseDetailsByIdCommand(graph.Purchase.Id, "Renamed",
            ["Stock", "Extra"], OccurredAt));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal("Renamed", response.Title);
        Assert.Equal(["Extra", "Stock"], response.Tags.Order());
        Assert.Equal(OccurredAt, response.OccurenceTime);
        Assert.Equal(FixedClock.Now, response.LastUpdatedTime);

        var purchase = await database.ReadAsync(c => c.Purchases.Include(p => p.Tags).SingleAsync());
        Assert.Equal("Renamed", purchase.Title);
        Assert.Equal(2, purchase.Tags.Count);
    }

    [Fact]
    public async Task UpdateDetailsById_WithoutATag_RemovesIt()
    {
        var graph = await SeededGraph.CreateAsync(database);

        ResultAssert.Succeeded(await UpdateDetailsAsync(new UpdatePurchaseDetailsByIdCommand(graph.Purchase.Id,
            "Renamed", [], OccurredAt)));

        var purchase = await database.ReadAsync(c => c.Purchases.Include(p => p.Tags).SingleAsync());
        Assert.Empty(purchase.Tags);
    }

    [Fact]
    public async Task UpdateDetailsById_WithUnknownId_ReturnsNotFound()
    {
        var result = await UpdateDetailsAsync(
            new UpdatePurchaseDetailsByIdCommand(Guid.NewGuid(), "x", [], OccurredAt));

        ResultAssert.Failed(result, "Purchase.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithExistingEntry_ChangesQuantity()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(graph.Purchase.Id,
            [new UpdateEntry(graph.PurchaseEntry.Id, graph.Product.Id, 5)]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(5, Assert.Single(response.ProductEntries).Quantity);
        Assert.Equal(50m, response.TotalPrice);
        Assert.Equal(FixedClock.Now, response.LastUpdatedTime);
    }

    [Fact]
    public async Task UpdateEntriesById_WithNewEntry_AddsItAtCurrentPrice()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(graph.Purchase.Id,
        [
            new UpdateEntry(graph.PurchaseEntry.Id, graph.Product.Id, 3),
            new UpdateEntry(null, graph.OtherProduct.Id, 2),
        ]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(2, response.ProductEntries.Length);
        Assert.Equal(38m, response.TotalPrice);
    }

    [Fact]
    public async Task UpdateEntriesById_OmittingAnEntry_RemovesIt()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(graph.Purchase.Id,
            [new UpdateEntry(null, graph.OtherProduct.Id, 1)]));

        var entry = Assert.Single(ResultAssert.Succeeded(result).ProductEntries);
        Assert.Equal(graph.OtherProduct.Id, entry.ProductId);
        Assert.False(await database.ReadAsync(c => c.PurchaseProductEntries
            .AnyAsync(e => e.Id == graph.PurchaseEntry.Id)));
    }

    [Fact]
    public async Task UpdateEntriesById_WithUnknownPurchase_ReturnsNotFound()
    {
        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(Guid.NewGuid(), []));

        ResultAssert.Failed(result, "Purchase.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithEntryFromElsewhere_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(graph.Purchase.Id,
            [new UpdateEntry(Guid.NewGuid(), graph.Product.Id, 1)]));

        ResultAssert.Failed(result, "Purchase.NonExistingEntry(s)", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithUnknownProduct_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(graph.Purchase.Id,
            [new UpdateEntry(null, Guid.NewGuid(), 1)]));

        ResultAssert.Failed(result, "Purchase.NonExistingProduct(s)", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithNewProductListedTwice_KeepsTotalQuantity()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(graph.Purchase.Id,
        [
            new UpdateEntry(null, graph.OtherProduct.Id, 2),
            new UpdateEntry(null, graph.OtherProduct.Id, 3),
        ]));

        Assert.Equal(5, Assert.Single(ResultAssert.Succeeded(result).ProductEntries).Quantity);
    }

    [Fact]
    public async Task UpdateEntriesById_WithTwoEntriesOfSameProduct_UpdatesEachEntry()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var entryId = graph.PurchaseEntry.Id;
        var productId = graph.Product.Id;
        var added = ResultAssert.Succeeded(await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(
            graph.Purchase.Id, [new UpdateEntry(entryId, productId, 3), new UpdateEntry(null, productId, 2)])));
        var addedId = added.ProductEntries.Single(e => e.Id != entryId).Id;

        var result = await UpdateEntriesAsync(new UpdatePurchaseEntriesByIdCommand(
            graph.Purchase.Id, [new UpdateEntry(entryId, productId, 7), new UpdateEntry(addedId, productId, 8)]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(7, response.ProductEntries.Single(e => e.Id == entryId).Quantity);
        Assert.Equal(8, response.ProductEntries.Single(e => e.Id == addedId).Quantity);
        Assert.Equal(150m, response.TotalPrice);
    }

    [Fact]
    public void GetQuery_ToString_IncludesOnlyProvidedFilters()
    {
        var query = new GetPurchaseQuery("abc", SortOrder.DESC, new DateOnly(2026, 1, 2), null, 5m, null, 2, 20);

        var text = query.ToString();

        Assert.Contains("Search: abc", text);
        Assert.Contains("SortOrder: DESC", text);
        Assert.Contains("OccurenceFrom:", text);
        Assert.DoesNotContain("OccurenceTo:", text);
        Assert.Contains("TotalFrom: 5", text);
        Assert.DoesNotContain("TotalTo:", text);
        Assert.Contains("Page: 2, PageSize: 20", text);
    }

    private static GetPurchaseQuery Query(string? search = null, SortOrder order = SortOrder.ASC, DateOnly? from = null,
        DateOnly? to = null, decimal? totalFrom = null, decimal? totalTo = null)
    {
        return new GetPurchaseQuery(search, order, from, to, totalFrom, totalTo);
    }

    private async Task<Result<CreatePurchaseResponse>> CreateAsync(CreatePurchaseCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new CreatePurchaseCommandHandler(context, new FixedClock(),
            NullLogger<CreatePurchaseCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private async Task<PagedResponse<PurchaseResponse>> GetAsync(GetPurchaseQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetPurchaseQueryHandler(context, NullLogger<GetPurchaseQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<UpdatePurchaseDetailsByIdResponse>> UpdateDetailsAsync(
        UpdatePurchaseDetailsByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdatePurchaseDetailsByIdCommandHandler(context, new FixedClock(),
            NullLogger<UpdatePurchaseDetailsByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private async Task<Result<UpdatePurchaseEntriesByIdResponse>> UpdateEntriesAsync(
        UpdatePurchaseEntriesByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdatePurchaseEntriesByIdCommandHandler(context, new FixedClock(),
            NullLogger<UpdatePurchaseEntriesByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }
}
