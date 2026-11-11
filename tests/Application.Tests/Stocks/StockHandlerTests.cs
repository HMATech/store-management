using Application.Common;
using Application.Stocks;
using Application.Stocks.Get;
using Application.Stocks.GetByProductId;
using Application.Stocks.StockAdjustments;
using Application.Stocks.StockAdjustments.Create;
using Application.Stocks.StockAdjustments.DeleteById;
using Application.Stocks.StockAdjustments.Get;
using Application.Stocks.StockAdjustments.GetById;
using Application.Tests.TestSupport;
using Domain.Products;
using Domain.Purchases;
using Domain.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;

namespace Application.Tests.Stocks;

public class StockHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task GetByProductId_SumsPurchasesSalesAndAdjustmentsAcrossLines()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var purchase = Purchase.CreateNew("Second restock", SeededGraph.OccurredAt, FixedClock.Now);
        purchase.UpdateProductEntries(
        [
            PurchaseProductEntry.CreateNew(purchase.Id, graph.Product.Id, 6m, 5),
            PurchaseProductEntry.CreateNew(purchase.Id, graph.Product.Id, 5m, 2),
        ]);
        await database.SeedAsync(purchase,
            StockAdjustment.CreateNew(graph.Product.Id, 4, "Found in storage", SeededGraph.OccurredAt, FixedClock.Now));

        var stock = ResultAssert.Succeeded(await GetByProductIdAsync(graph.Product.Id));

        Assert.Equal(new ProductStockResponse(graph.Product.Id, "Milk", Purchased: 10, Sold: 3, Adjusted: 3,
            MinimumQuantity: StockDefinition.DefaultMinimumQuantity), stock);
        Assert.Equal(10, stock.Quantity);
        Assert.Equal(StockStatus.Normal, stock.Status);
    }

    [Fact]
    public async Task GetByProductId_IgnoresOtherProductsMovements()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var stock = ResultAssert.Succeeded(await GetByProductIdAsync(graph.OtherProduct.Id));

        Assert.Equal(new ProductStockResponse(graph.OtherProduct.Id, "Butter", 0, 0, 0,
            StockDefinition.DefaultMinimumQuantity), stock);
        Assert.Equal(0, stock.Quantity);
    }

    [Fact]
    public async Task GetByProductId_CanGoNegativeWhenMoreIsSoldThanBought()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var stock = ResultAssert.Succeeded(await GetByProductIdAsync(graph.Product.Id));

        // The seed buys 3, sells 3 and adjusts by -1, so a sale recorded before its purchase shows up as negative.
        Assert.Equal(-1, stock.Quantity);
    }

    [Fact]
    public async Task GetByProductId_WithUnknownProduct_ReturnsNotFound()
    {
        await SeededGraph.CreateAsync(database);

        var result = await GetByProductIdAsync(Guid.NewGuid());

        ResultAssert.Failed(result, "Products.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_ReturnsEveryProductSortedByName()
    {
        await SeededGraph.CreateAsync(database);

        var page = ResultAssert.Succeeded(await GetAsync(new GetStockQuery(null, SortOrder.ASC)));

        Assert.Equal(["Butter", "Milk"], page.Responses.Select(s => s.ProductName));
        Assert.Equal([0L, -1L], page.Responses.Select(s => s.Quantity));
    }

    [Fact]
    public async Task Get_SortsDescending()
    {
        await SeededGraph.CreateAsync(database);

        var page = ResultAssert.Succeeded(await GetAsync(new GetStockQuery(null, SortOrder.DESC)));

        Assert.Equal(["Milk", "Butter"], page.Responses.Select(s => s.ProductName));
    }

    [Fact]
    public async Task Get_FiltersBySearch()
    {
        await SeededGraph.CreateAsync(database);

        var page = ResultAssert.Succeeded(await GetAsync(new GetStockQuery("Mil", SortOrder.ASC)));

        Assert.Equal("Milk", Assert.Single(page.Responses).ProductName);
    }

    [Fact]
    public async Task Get_Pages()
    {
        await SeededGraph.CreateAsync(database);

        var page = ResultAssert.Succeeded(await GetAsync(new GetStockQuery(null, SortOrder.ASC, Page: 2, PageSize: 1)));

        Assert.Equal("Milk", Assert.Single(page.Responses).ProductName);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task Get_ReportsEachProductsStatus()
    {
        await SeededGraph.CreateAsync(database);

        var page = ResultAssert.Succeeded(await GetAsync(new GetStockQuery(null, SortOrder.ASC)));

        // Milk is at -1 and Butter at 0, which is at or below the default minimum.
        Assert.Equal([StockStatus.Low, StockStatus.Negative], page.Responses.Select(s => s.Status));
    }

    [Theory]
    [InlineData("negative", "Milk")]
    [InlineData("LOW", "Butter")]
    [InlineData("Normal", "Cheese")]
    public async Task Get_FiltersByStatusIgnoringCase(string status, string expected)
    {
        var graph = await SeededGraph.CreateAsync(database);
        await AddProductWithStockAsync(graph, "Cheese", stock: 10, minimum: 2);

        var page = ResultAssert.Succeeded(await GetAsync(new GetStockQuery(null, SortOrder.ASC, Status: status)));

        Assert.Equal(expected, Assert.Single(page.Responses).ProductName);
        Assert.Equal(1, page.TotalCount);
    }

    [Theory]
    [InlineData(3, 3, StockStatus.Low)]
    [InlineData(4, 3, StockStatus.Normal)]
    [InlineData(0, 0, StockStatus.Low)]
    [InlineData(1, 0, StockStatus.Normal)]
    public async Task Get_JudgesStatusAgainstTheProductsOwnMinimum(int stock, int minimum, StockStatus expected)
    {
        var graph = await SeededGraph.CreateAsync(database);
        await AddProductWithStockAsync(graph, "Cheese", stock, minimum);

        var page = ResultAssert.Succeeded(await GetAsync(new GetStockQuery("Cheese", SortOrder.ASC)));

        var cheese = Assert.Single(page.Responses);
        Assert.Equal(minimum, cheese.MinimumQuantity);
        Assert.Equal(expected, cheese.Status);
    }

    [Theory]
    [InlineData(-1, 5, StockStatus.Negative)]
    [InlineData(0, 5, StockStatus.Low)]
    [InlineData(5, 5, StockStatus.Low)]
    [InlineData(6, 5, StockStatus.Normal)]
    [InlineData(0, 0, StockStatus.Low)]
    public void StatusOf_ComparesQuantityWithMinimum(long quantity, int minimum, StockStatus expected)
    {
        Assert.Equal(expected, StockQueries.StatusOf(quantity, minimum));
    }

    [Theory]
    [InlineData("low", true, StockStatus.Low)]
    [InlineData("NEGATIVE", true, StockStatus.Negative)]
    [InlineData("1", false, StockStatus.Normal)]
    [InlineData("", false, StockStatus.Normal)]
    [InlineData(null, false, StockStatus.Normal)]
    [InlineData("empty", false, StockStatus.Normal)]
    public void TryParseStatus_AcceptsOnlyNames(string? value, bool expected, StockStatus status)
    {
        Assert.Equal(expected, StockQueries.TryParseStatus(value, out var parsed));
        Assert.Equal(status, parsed);
    }

    [Fact]
    public async Task GetByProductId_ReportsStatusAndMinimum()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var stock = ResultAssert.Succeeded(await GetByProductIdAsync(graph.Product.Id));

        Assert.Equal(StockStatus.Negative, stock.Status);
        Assert.Equal(StockDefinition.DefaultMinimumQuantity, stock.MinimumQuantity);
    }

    [Fact]
    public async Task CreateAdjustment_PersistsItAndChangesStock()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new CreateStockAdjustmentCommandHandler(context, new FixedClock(),
            NullLogger<CreateStockAdjustmentCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(
            new CreateStockAdjustmentCommand(graph.Product.Id, 5, "Stock count", SeededGraph.OccurredAt), default));

        var adjustment = await database.ReadAsync(c => c.StockAdjustments.SingleAsync(a => a.Id == response.Id));
        Assert.Equal(graph.Product.Id, adjustment.ProductId);
        Assert.Equal(5, adjustment.Quantity);
        Assert.Equal("Stock count", adjustment.Reason);
        Assert.Equal(SeededGraph.OccurredAt, adjustment.OccurrenceTime);
        Assert.Equal(FixedClock.Now, adjustment.CreatedTime);
        Assert.Equal(4, ResultAssert.Succeeded(await GetByProductIdAsync(graph.Product.Id)).Quantity);
    }

    [Fact]
    public async Task CreateAdjustment_WithUnknownProduct_ReturnsNotFoundAndSavesNothing()
    {
        await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new CreateStockAdjustmentCommandHandler(context, new FixedClock(),
            NullLogger<CreateStockAdjustmentCommandHandler>.Instance);

        var result = await handler.HandleAsync(
            new CreateStockAdjustmentCommand(Guid.NewGuid(), 5, "Stock count", SeededGraph.OccurredAt), default);

        ResultAssert.Failed(result, "Products.NotFound", ErrorType.NotFound);
        Assert.Equal(1, await database.ReadAsync(c => c.StockAdjustments.CountAsync()));
    }

    [Fact]
    public async Task GetAdjustments_ForProduct_ReturnsOnlyItsAdjustmentsNewestFirst()
    {
        var graph = await SeededGraph.CreateAsync(database);
        var newer = StockAdjustment.CreateNew(graph.Product.Id, 2, "Newer", SeededGraph.OccurredAt.AddDays(1),
            FixedClock.Now);
        var other = StockAdjustment.CreateNew(graph.OtherProduct.Id, 1, "Other", SeededGraph.OccurredAt,
            FixedClock.Now);
        await database.SeedAsync(newer, other);

        var page = ResultAssert.Succeeded(await GetAdjustmentsAsync(new GetStockAdjustmentQuery(graph.Product.Id)));

        Assert.Equal([newer.Id, graph.StockAdjustment.Id], page.Responses.Select(a => a.Id));
    }

    [Fact]
    public async Task GetAdjustments_WithoutProduct_ReturnsAll()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await database.SeedAsync(StockAdjustment.CreateNew(graph.OtherProduct.Id, 1, "Other", SeededGraph.OccurredAt,
            FixedClock.Now));

        var page = ResultAssert.Succeeded(await GetAdjustmentsAsync(new GetStockAdjustmentQuery(null)));

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetAdjustments_ForUnknownProduct_ReturnsEmptyPage()
    {
        await SeededGraph.CreateAsync(database);

        var page = ResultAssert.Succeeded(await GetAdjustmentsAsync(new GetStockAdjustmentQuery(Guid.NewGuid())));

        Assert.Empty(page.Responses);
    }

    [Fact]
    public async Task GetAdjustmentById_ReturnsIt()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new GetStockAdjustmentByIdQueryHandler(context,
            NullLogger<GetStockAdjustmentByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(
            await handler.HandleAsync(new GetStockAdjustmentByIdQuery(graph.StockAdjustment.Id), default));

        Assert.Equal(graph.StockAdjustment.ToStockAdjustmentResponse(), response);
    }

    [Fact]
    public async Task GetAdjustmentById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetStockAdjustmentByIdQueryHandler(context,
            NullLogger<GetStockAdjustmentByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetStockAdjustmentByIdQuery(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Stocks.AdjustmentNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteAdjustmentById_RemovesItAndRestoresStock()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new DeleteStockAdjustmentByIdCommandHandler(context,
            NullLogger<DeleteStockAdjustmentByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(
            await handler.HandleAsync(new DeleteStockAdjustmentByIdCommand(graph.StockAdjustment.Id), default));

        Assert.False(await database.ReadAsync(c => c.StockAdjustments.AnyAsync()));
        Assert.Equal(0, ResultAssert.Succeeded(await GetByProductIdAsync(graph.Product.Id)).Quantity);
    }

    [Fact]
    public async Task DeleteAdjustmentById_WithUnknownId_ReturnsNotFound()
    {
        await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new DeleteStockAdjustmentByIdCommandHandler(context,
            NullLogger<DeleteStockAdjustmentByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteStockAdjustmentByIdCommand(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Stocks.AdjustmentNotFound", ErrorType.NotFound);
        Assert.Equal(1, await database.ReadAsync(c => c.StockAdjustments.CountAsync()));
    }

    private async Task AddProductWithStockAsync(SeededGraph graph, string name, int stock, int minimum)
    {
        var product = Product.CreateNew(name, null, graph.Brand.Id, 1f, graph.Unit.Id, FixedClock.Now,
            StockDefinition.CreateNew(minimum));
        var purchase = Purchase.CreateNew("Stock " + name, SeededGraph.OccurredAt, FixedClock.Now);
        purchase.UpdateProductEntries([PurchaseProductEntry.CreateNew(purchase.Id, product.Id, 1m, stock)]);
        await database.SeedAsync(product, purchase);
    }

    private async Task<Result<ProductStockResponse>> GetByProductIdAsync(Guid productId)
    {
        await using var context = database.CreateContext();
        var handler = new GetStockByProductIdQueryHandler(context,
            NullLogger<GetStockByProductIdQueryHandler>.Instance);
        return await handler.HandleAsync(new GetStockByProductIdQuery(productId), default);
    }

    private async Task<Result<PagedResponse<ProductStockResponse>>> GetAsync(GetStockQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetStockQueryHandler(context, NullLogger<GetStockQueryHandler>.Instance);
        return await handler.HandleAsync(query, default);
    }

    private async Task<Result<PagedResponse<StockAdjustmentResponse>>> GetAdjustmentsAsync(
        GetStockAdjustmentQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetStockAdjustmentQueryHandler(context, NullLogger<GetStockAdjustmentQueryHandler>.Instance);
        return await handler.HandleAsync(query, default);
    }
}
