using Application.Common;
using Application.Products.ProductPrices;
using Application.Products.ProductPrices.DeleteById;
using Application.Products.ProductPrices.Get;
using Application.Products.ProductPrices.GetById;
using Application.Products.ProductPrices.UpdateById;
using Application.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;

namespace Application.Tests.Products;

public class ProductPriceHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task DeleteById_WithClosedPrice_RemovesIt()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new DeleteProductPriceByIdCommandHandler(context,
            NullLogger<DeleteProductPriceByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(
            await handler.HandleAsync(new DeleteProductPriceByIdCommand(graph.OldPrice.Id), default));

        Assert.False(await database.ReadAsync(c => c.ProductPrices.AnyAsync(p => p.Id == graph.OldPrice.Id)));
    }

    [Fact]
    public async Task DeleteById_WithCurrentPrice_ReturnsConflict()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new DeleteProductPriceByIdCommandHandler(context,
            NullLogger<DeleteProductPriceByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteProductPriceByIdCommand(graph.CurrentPrice.Id), default);

        ResultAssert.Failed(result, "Products.PriceInUse", ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteProductPriceByIdCommandHandler(context,
            NullLogger<DeleteProductPriceByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteProductPriceByIdCommand(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Products.PriceNotFound", ErrorType.NotFound);
    }

    [Theory]
    [InlineData("MILK", 2)]
    [InlineData("acme", 3)]
    [InlineData("missing", 0)]
    [InlineData(null, 3)]
    public async Task Get_SearchesProductAndBrandNameIgnoringCase(string? search, int expectedCount)
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetProductPriceQuery(search, SortOrder.ASC));

        Assert.Equal(expectedCount, value.TotalCount);
    }

    [Fact]
    public async Task Get_SortsByValidFromTime()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var ascending = await GetAsync(new GetProductPriceQuery("Milk", SortOrder.ASC));
        var descending = await GetAsync(new GetProductPriceQuery("Milk", SortOrder.DESC));

        Assert.Equal([graph.OldPrice.Id, graph.CurrentPrice.Id], ascending.Responses.Select(p => p.Id));
        Assert.Equal([graph.CurrentPrice.Id, graph.OldPrice.Id], descending.Responses.Select(p => p.Id));
    }

    [Fact]
    public async Task GetById_WithExistingPrice_ReturnsIt()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new GetProductPriceByIdQueryHandler(context,
            NullLogger<GetProductPriceByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(
            await handler.HandleAsync(new GetProductPriceByIdQuery(graph.OldPrice.Id), default));

        Assert.Equal(graph.OldPrice.Id, response.Id);
        Assert.Equal(graph.Product.Id, response.ProductId);
        Assert.Equal(SeededGraph.OldPriceValue, response.Value);
        Assert.Equal(graph.OldPrice.ValidFromTime, response.ValidFromTime);
        Assert.Equal(graph.OldPrice.ValidToTime, response.ValidToTime);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetProductPriceByIdQueryHandler(context,
            NullLogger<GetProductPriceByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetProductPriceByIdQuery(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Products.PriceNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithExistingPrice_ChangesValue()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new UpdateProductPriceByIdCommandHandler(context,
            NullLogger<UpdateProductPriceByIdCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(
            new UpdateProductPriceByIdCommand(graph.OldPrice.Id, 9m), default));

        Assert.Equal(graph.OldPrice.Id, response.Id);
        Assert.Equal(9m, response.Value);
        var saved = await database.ReadAsync(c => c.ProductPrices.SingleAsync(p => p.Id == graph.OldPrice.Id));
        Assert.Equal(9m, saved.Value);
    }

    [Fact]
    public async Task UpdateById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new UpdateProductPriceByIdCommandHandler(context,
            NullLogger<UpdateProductPriceByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new UpdateProductPriceByIdCommand(Guid.NewGuid(), 9m), default);

        ResultAssert.Failed(result, "Products.PriceNotFound", ErrorType.NotFound);
    }

    private async Task<PagedResponse<ProductPriceResponse>> GetAsync(GetProductPriceQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetProductPriceQueryHandler(context, NullLogger<GetProductPriceQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }
}
