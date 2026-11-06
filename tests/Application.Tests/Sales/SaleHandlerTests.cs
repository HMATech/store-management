using Application.Common;
using Application.Sales;
using Application.Sales.Create;
using Application.Sales.DeleteById;
using Application.Sales.Get;
using Application.Sales.GetById;
using Application.Sales.UpdateDetailsById;
using Application.Sales.UpdateEntriesById;
using Application.Tests.TestSupport;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using CreateEntry = Application.Sales.Create.ProductEntryCommand;
using UpdateEntry = Application.Sales.UpdateEntriesById.ProductEntryCommand;

namespace Application.Tests.Sales;

public class SaleHandlerTests
{
    private static readonly DateTime OccurredAt = new(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly TestDatabase database = new();

    [Fact]
    public async Task Create_WithValidInput_UsesCurrentPriceOfEachProduct()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await CreateAsync(new CreateSaleCommand("New", ["Retail", "Urgent"],
            [new CreateEntry(graph.Product.Id, 2), new CreateEntry(graph.OtherProduct.Id, 1)], OccurredAt));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(FixedClock.Now, response.CreatedTime);

        var sale = await database.ReadAsync(c => c.Sales
            .Include(p => p.Products)
            .Include(p => p.Tags)
            .SingleAsync(p => p.Id == response.Id));
        Assert.Equal("New", sale.Title);
        Assert.Equal(OccurredAt, sale.OccurenceTime);
        Assert.Equal(["retail", "urgent"], sale.Tags.Select(t => t.Name.Normalized).Order());
        Assert.Equal(2, sale.Products.Count);
        var entry = sale.Products.Single(e => e.ProductId == graph.Product.Id);
        Assert.Equal(graph.CurrentPrice.Id, entry.PriceId);
        Assert.Equal(2, entry.Quantity);
    }

    [Fact]
    public async Task Create_ReusesExistingTagInsteadOfDuplicatingIt()
    {
        var graph = await SeededGraph.CreateAsync(database);

        ResultAssert.Succeeded(await CreateAsync(new CreateSaleCommand("New", ["RETAIL"],
            [new CreateEntry(graph.Product.Id, 1)], OccurredAt)));

        Assert.Equal(1, await database.ReadAsync(c => c.SaleTags.CountAsync()));
    }

    [Fact]
    public async Task Create_WithoutTags_Succeeds()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var response = ResultAssert.Succeeded(await CreateAsync(new CreateSaleCommand("New", [],
            [new CreateEntry(graph.Product.Id, 1)], OccurredAt)));

        var sale = await database.ReadAsync(c => c.Sales
            .Include(p => p.Tags)
            .SingleAsync(p => p.Id == response.Id));
        Assert.Empty(sale.Tags);
    }

    [Fact]
    public async Task Create_WithUnknownProduct_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var unknownId = Guid.NewGuid();

        var result = await CreateAsync(new CreateSaleCommand("New", [],
            [new CreateEntry(graph.Product.Id, 1), new CreateEntry(unknownId, 1)], OccurredAt));

        ResultAssert.Failed(result, "Sale.NonExistingProduct(s)", ErrorType.NotFound);
        Assert.Contains(unknownId.ToString(), result.Error.Description);
    }

    [Fact]
    public async Task Create_WithSameProductTwice_KeepsTotalQuantity()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var response = ResultAssert.Succeeded(await CreateAsync(new CreateSaleCommand("New", [],
            [new CreateEntry(graph.Product.Id, 2), new CreateEntry(graph.Product.Id, 3)], OccurredAt)));

        var entries = await database.ReadAsync(c => c.SaleProductEntries
            .Where(e => e.SaleId == response.Id)
            .ToListAsync());
        Assert.Equal(5, entries.Sum(e => e.Quantity));
    }

    [Fact]
    public async Task DeleteById_WithExistingSale_RemovesIt()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new DeleteSaleByIdCommandHandler(context,
            NullLogger<DeleteSaleByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeleteSaleByIdCommand(graph.Sale.Id), default));

        Assert.False(await database.ReadAsync(c => c.Sales.AnyAsync()));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteSaleByIdCommandHandler(context,
            NullLogger<DeleteSaleByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteSaleByIdCommand(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Sale.NotFound", ErrorType.NotFound);
    }

    [Theory]
    [InlineData("MORNING", 1)]
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
        var later = Domain.Sales.Sale.CreateNew("Later", OccurredAt, FixedClock.Now);
        await database.SeedAsync(later);

        var ascending = await GetAsync(Query());
        var descending = await GetAsync(Query(order: SortOrder.DESC));

        Assert.Equal(["Morning sale", "Later"], ascending.Responses.Select(p => p.Title));
        Assert.Equal(["Later", "Morning sale"], descending.Responses.Select(p => p.Title));
    }

    [Fact]
    public async Task GetById_ReturnsSaleWithEntriesAndTotals()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new GetSaleByIdQueryHandler(context, NullLogger<GetSaleByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(
            await handler.HandleAsync(new GetSaleByIdQuery(graph.Sale.Id), default));

        Assert.Equal("Morning sale", response.Title);
        Assert.Equal(["Retail"], response.Tags);
        Assert.Equal(30m, response.TotalPrice);
        Assert.Equal(SeededGraph.OccurredAt, response.OccurrenceTime);
        Assert.Equal(new ProductEntryResponse(graph.SaleEntry.Id, graph.Product.Id, "Milk",
            graph.CurrentPrice.Id, 10m, 3, 30m), Assert.Single(response.ProductEntries));
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetSaleByIdQueryHandler(context, NullLogger<GetSaleByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetSaleByIdQuery(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Sale.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateDetailsById_ChangesTitleTagsAndOccurrence()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateDetailsAsync(new UpdateSaleDetailsByIdCommand(graph.Sale.Id, "Renamed",
            ["Retail", "Extra"], OccurredAt));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal("Renamed", response.Title);
        Assert.Equal(["Extra", "Retail"], response.Tags.Order());
        Assert.Equal(OccurredAt, response.OccurenceTime);
        Assert.Equal(FixedClock.Now, response.LastUpdatedTime);

        var sale = await database.ReadAsync(c => c.Sales.Include(p => p.Tags).SingleAsync());
        Assert.Equal("Renamed", sale.Title);
        Assert.Equal(2, sale.Tags.Count);
    }

    [Fact]
    public async Task UpdateDetailsById_WithoutATag_RemovesIt()
    {
        var graph = await SeededGraph.CreateAsync(database);

        ResultAssert.Succeeded(await UpdateDetailsAsync(new UpdateSaleDetailsByIdCommand(graph.Sale.Id,
            "Renamed", [], OccurredAt)));

        var sale = await database.ReadAsync(c => c.Sales.Include(p => p.Tags).SingleAsync());
        Assert.Empty(sale.Tags);
    }

    [Fact]
    public async Task UpdateDetailsById_WithUnknownId_ReturnsNotFound()
    {
        var result = await UpdateDetailsAsync(new UpdateSaleDetailsByIdCommand(Guid.NewGuid(), "x", [], OccurredAt));

        ResultAssert.Failed(result, "Sale.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithExistingEntry_ChangesQuantity()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
            [new UpdateEntry(graph.SaleEntry.Id, graph.Product.Id, 5)]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(5, Assert.Single(response.ProductEntries).Quantity);
        Assert.Equal(50m, response.TotalPrice);
        Assert.Equal(FixedClock.Now, response.LastUpdatedTime);
    }

    [Fact]
    public async Task UpdateEntriesById_WithNewEntry_AddsItAtCurrentPrice()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
        [
            new UpdateEntry(graph.SaleEntry.Id, graph.Product.Id, 3),
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

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
            [new UpdateEntry(null, graph.OtherProduct.Id, 1)]));

        var entry = Assert.Single(ResultAssert.Succeeded(result).ProductEntries);
        Assert.Equal(graph.OtherProduct.Id, entry.ProductId);
        Assert.False(await database.ReadAsync(c => c.SaleProductEntries
            .AnyAsync(e => e.Id == graph.SaleEntry.Id)));
    }

    [Fact]
    public async Task UpdateEntriesById_WithUnknownSale_ReturnsNotFound()
    {
        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(Guid.NewGuid(), []));

        ResultAssert.Failed(result, "Sale.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithEntryFromElsewhere_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
            [new UpdateEntry(Guid.NewGuid(), graph.Product.Id, 1)]));

        ResultAssert.Failed(result, "Sale.NonExistingEntry(s)", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithUnknownProduct_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
            [new UpdateEntry(null, Guid.NewGuid(), 1)]));

        ResultAssert.Failed(result, "Sale.NonExistingProduct(s)", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateEntriesById_WithEmptyEntryId_AddsNewEntry()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
            [new UpdateEntry(Guid.Empty, graph.OtherProduct.Id, 1)]));

        Assert.Equal(graph.OtherProduct.Id, Assert.Single(ResultAssert.Succeeded(result).ProductEntries).ProductId);
    }

    [Fact]
    public async Task UpdateEntriesById_WithNewProductListedTwice_KeepsTotalQuantity()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
        [
            new UpdateEntry(null, graph.OtherProduct.Id, 2),
            new UpdateEntry(null, graph.OtherProduct.Id, 3),
        ]));

        Assert.Equal(5, Assert.Single(ResultAssert.Succeeded(result).ProductEntries).Quantity);
    }

    [Fact]
    public async Task UpdateEntriesById_WithNewEntryForKeptProduct_AddsToThatEntry()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var entryId = graph.SaleEntry.Id;
        var productId = graph.Product.Id;

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(
            graph.Sale.Id, [new UpdateEntry(entryId, productId, 3), new UpdateEntry(null, productId, 2)]));

        var response = ResultAssert.Succeeded(result);
        var entry = Assert.Single(response.ProductEntries);
        Assert.Equal(entryId, entry.Id);
        Assert.Equal(5, entry.Quantity);
        Assert.Equal(50m, response.TotalPrice);
    }

    [Fact]
    public async Task UpdateEntriesById_WithNewEntryForOmittedProduct_ReplacesTheEntry()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(
            graph.Sale.Id, [new UpdateEntry(null, graph.Product.Id, 2)]));

        var entry = Assert.Single(ResultAssert.Succeeded(result).ProductEntries);
        Assert.NotEqual(graph.SaleEntry.Id, entry.Id);
        Assert.Equal(2, entry.Quantity);
    }

    [Fact]
    public async Task UpdateEntriesById_WithSameEntryListedTwice_AddsQuantities()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var duplicate = new UpdateEntry(graph.SaleEntry.Id, graph.Product.Id, 3);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(
            graph.Sale.Id, [duplicate, duplicate]));

        Assert.Equal(6, Assert.Single(ResultAssert.Succeeded(result).ProductEntries).Quantity);
    }

    [Fact]
    public async Task UpdateEntriesById_WithNewEntryForLineAtOldPrice_AddsLineAtCurrentPrice()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var oldLine = await AddEntryAsync(graph, graph.OldPrice.Id, quantity: 3);

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(
            graph.Sale.Id,
            [new UpdateEntry(oldLine.Id, graph.Product.Id, 3), new UpdateEntry(null, graph.Product.Id, 2)]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(2, response.ProductEntries.Length);
        Assert.Equal(3, response.ProductEntries.Single(e => e.Id == oldLine.Id).Quantity);
        Assert.Equal(2, response.ProductEntries.Single(e => e.Id != oldLine.Id).Quantity);
        Assert.Equal(44m, response.TotalPrice);
    }

    [Fact]
    public async Task UpdateEntriesById_WithLinesAtOldAndCurrentPrice_AddsToCurrentPriceLine()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var oldLine = await AddEntryAsync(graph, graph.OldPrice.Id, quantity: 1);
        var productId = graph.Product.Id;

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
        [
            new UpdateEntry(graph.SaleEntry.Id, productId, 7),
            new UpdateEntry(oldLine.Id, productId, 8),
            new UpdateEntry(null, productId, 2),
        ]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(2, response.ProductEntries.Length);
        Assert.Equal(9, response.ProductEntries.Single(e => e.Id == graph.SaleEntry.Id).Quantity);
        Assert.Equal(8, response.ProductEntries.Single(e => e.Id == oldLine.Id).Quantity);
        Assert.Equal(154m, response.TotalPrice);
    }

    [Fact]
    public async Task UpdateEntriesById_WithTwoLinesAtCurrentPrice_AddsNewQuantityOnce()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var secondLine = await AddEntryAsync(graph, graph.CurrentPrice.Id, quantity: 1);
        var productId = graph.Product.Id;

        var result = await UpdateEntriesAsync(new UpdateSaleEntriesByIdCommand(graph.Sale.Id,
        [
            new UpdateEntry(graph.SaleEntry.Id, productId, 7),
            new UpdateEntry(secondLine.Id, productId, 8),
            new UpdateEntry(null, productId, 2),
        ]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal(2, response.ProductEntries.Length);
        Assert.Equal(17, response.ProductEntries.Sum(e => e.Quantity));
    }

    [Fact]
    public void GetQuery_ToString_IncludesOnlyProvidedFilters()
    {
        var query = new GetSaleQuery("abc", SortOrder.DESC, new DateOnly(2026, 1, 2), null, 5m, null, 2, 20);

        var text = query.ToString();

        Assert.Contains("Search: abc", text);
        Assert.Contains("SortOrder: DESC", text);
        Assert.Contains("OccurenceFrom:", text);
        Assert.DoesNotContain("OccurenceTo:", text);
        Assert.Contains("TotalFrom: 5", text);
        Assert.DoesNotContain("TotalTo:", text);
        Assert.Contains("Page: 2, PageSize: 20", text);
    }

    private static GetSaleQuery Query(string? search = null, SortOrder order = SortOrder.ASC, DateOnly? from = null,
        DateOnly? to = null, decimal? totalFrom = null, decimal? totalTo = null)
    {
        return new GetSaleQuery(search, order, from, to, totalFrom, totalTo);
    }

    private async Task<Result<CreateSaleResponse>> CreateAsync(CreateSaleCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new CreateSaleCommandHandler(context, new FixedClock(),
            NullLogger<CreateSaleCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private async Task<PagedResponse<SaleResponse>> GetAsync(GetSaleQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetSaleQueryHandler(context, NullLogger<GetSaleQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<UpdateSaleDetailsByIdResponse>> UpdateDetailsAsync(
        UpdateSaleDetailsByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdateSaleDetailsByIdCommandHandler(context, new FixedClock(),
            NullLogger<UpdateSaleDetailsByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private async Task<SaleProductEntry> AddEntryAsync(SeededGraph graph, Guid priceId, int quantity)
    {
        await using var context = database.CreateContext();
        var entry = SaleProductEntry.CreateNew(graph.Sale.Id, graph.Product.Id, priceId, quantity);
        context.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private async Task<Result<UpdateSaleEntriesByIdResponse>> UpdateEntriesAsync(
        UpdateSaleEntriesByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdateSaleEntriesByIdCommandHandler(context, new FixedClock(),
            NullLogger<UpdateSaleEntriesByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }
}
