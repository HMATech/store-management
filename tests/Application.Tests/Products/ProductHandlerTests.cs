using Application.Common;
using Application.Products;
using Application.Products.Create;
using Application.Products.DeleteById;
using Application.Products.Get;
using Application.Products.GetById;
using Application.Products.UpdateById;
using Application.Products.UpdatePriceById;
using Application.Tests.TestSupport;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;

namespace Application.Tests.Products;

public class ProductHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task Create_WithValidInput_PersistsProductWithPriceAndTags()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await CreateAsync(new CreateProductCommand("Cheese", "Aged", 2f, graph.Unit.Id, graph.Brand.Id,
            12.5m, ["Dairy", "Imported"]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal("Cheese", response.Name);
        Assert.Equal("Aged", response.Description);
        Assert.Equal(2f, response.Measure);
        Assert.Equal("Litre", response.Unit);
        Assert.Equal("Acme", response.Brand);
        Assert.Equal(12.5m.ToString(), response.Price);
        Assert.Equal(FixedClock.Now, response.CreatedTime);

        var product = await database.ReadAsync(c => c.Products
            .Include(p => p.Prices)
            .Include(p => p.Tags)
            .SingleAsync(p => p.Id == response.Id));
        var price = Assert.Single(product.Prices);
        Assert.Equal(12.5m, price.Value);
        Assert.Null(price.ValidToTime);
        Assert.Equal(["dairy", "imported"], product.Tags.Select(t => t.Name.Normalized).Order());
    }

    [Fact]
    public async Task Create_ReusesExistingTagInsteadOfDuplicatingIt()
    {
        var graph = await SeededGraph.CreateAsync(database);

        ResultAssert.Succeeded(await CreateAsync(new CreateProductCommand("Cheese", null, 2f, graph.Unit.Id,
            graph.Brand.Id, 1m, ["DAIRY"])));

        Assert.Equal(1, await database.ReadAsync(c => c.ProductTags.CountAsync()));
    }

    [Fact]
    public async Task Create_WithUnknownBrand_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await CreateAsync(new CreateProductCommand("Cheese", null, 2f, graph.Unit.Id, 999, 1m, []));

        ResultAssert.Failed(result, "Brands.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Create_WithUnknownUnit_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await CreateAsync(new CreateProductCommand("Cheese", null, 2f, 999, graph.Brand.Id, 1m, []));

        ResultAssert.Failed(result, "Products.UnitNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Create_WithSameNameAndBrand_ReturnsConflict()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await CreateAsync(new CreateProductCommand("Milk", null, 2f, graph.Unit.Id, graph.Brand.Id,
            1m, []));

        ResultAssert.Failed(result, "Products.AlreadyExist", ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteById_WithExistingProduct_RemovesIt()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new DeleteProductByIdCommandHandler(context,
            NullLogger<DeleteProductByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeleteProductByIdCommand(graph.OtherProduct.Id), default));

        Assert.False(await database.ReadAsync(c => c.Products.AnyAsync(p => p.Id == graph.OtherProduct.Id)));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteProductByIdCommandHandler(context,
            NullLogger<DeleteProductByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteProductByIdCommand(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Products.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_ReturnsProductWithItsCurrentPrice()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetProductQuery("Milk", SortOrder.ASC));

        var response = Assert.Single(value.Responses);
        Assert.Equal(graph.Product.Id, response.Id);
        Assert.Equal("Fresh milk", response.Description);
        Assert.Equal(1.5f, response.Measure);
        Assert.Equal(graph.Unit.Id, response.MeasureUnit.Id);
        Assert.Equal("Litre", response.MeasureUnit.Name);
        Assert.Equal("Acme", response.Brand);
        Assert.Equal(SeededGraph.CurrentPriceValue, response.Price);
        Assert.Equal(["Dairy"], response.Tags);
    }

    [Theory]
    [InlineData(SortOrder.ASC, new[] { "Butter", "Milk" })]
    [InlineData(SortOrder.DESC, new[] { "Milk", "Butter" })]
    public async Task Get_SortsByName(SortOrder order, string[] expected)
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetProductQuery(null, order));

        Assert.Equal(expected, value.Responses.Select(p => p.Name));
    }

    [Fact]
    public async Task Get_PagesResults()
    {
        await SeededGraph.CreateAsync(database);

        var value = await GetAsync(new GetProductQuery(null, SortOrder.ASC, Page: 1, PageSize: 1));

        Assert.Equal("Butter", Assert.Single(value.Responses).Name);
        Assert.Equal(2, value.TotalCount);
        Assert.True(value.HasNextPage);
    }

    [Fact]
    public async Task GetById_WithExistingProduct_ReturnsCurrentPrice()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new GetProductByIdQueryHandler(context, NullLogger<GetProductByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(
            await handler.HandleAsync(new GetProductByIdQuery(graph.Product.Id), default));

        Assert.Equal("Milk", response.Name);
        Assert.Equal(SeededGraph.CurrentPriceValue, response.Price);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetProductByIdQueryHandler(context, NullLogger<GetProductByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetProductByIdQuery(Guid.NewGuid()), default);

        ResultAssert.Failed(result, "Products.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithSamePrice_UpdatesFieldsWithoutAddingPrice()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateProductByIdCommand(graph.Product.Id, "Whole Milk", null, 3f,
            graph.OtherUnit.Id, SeededGraph.CurrentPriceValue, ["Fresh"]));

        var response = ResultAssert.Succeeded(result);
        Assert.Equal("Whole Milk", response.Name);
        Assert.Equal(3f, response.Measure);
        Assert.Equal("Gram", response.Unit);
        Assert.Equal(SeededGraph.CurrentPriceValue, response.Price);
        Assert.Equal(["Fresh"], response.Tags);

        var product = await database.ReadAsync(c => c.Products
            .Include(p => p.Prices)
            .Include(p => p.Detail)
            .SingleAsync(p => p.Id == graph.Product.Id));
        Assert.Equal(2, product.Prices.Count);
        Assert.Equal(graph.OtherUnit.Id, product.Detail.MeasureUnitId);
        Assert.Equal(FixedClock.Now, product.LastUpdatedTime);
    }

    [Fact]
    public async Task UpdateById_WithNewPrice_ClosesCurrentPriceAndAddsNewOne()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateProductByIdCommand(graph.Product.Id, "Milk", null, 1.5f,
            graph.Unit.Id, 20m, []));

        Assert.Equal(20m, ResultAssert.Succeeded(result).Price);

        var prices = await database.ReadAsync(c => c.ProductPrices
            .Where(p => p.ProductId == graph.Product.Id)
            .ToListAsync());
        Assert.Equal(3, prices.Count);
        Assert.Equal(FixedClock.Now, prices.Single(p => p.Id == graph.CurrentPrice.Id).ValidToTime);
        Assert.Null(prices.Single(p => p.Value == 20m).ValidToTime);
    }

    [Fact]
    public async Task UpdateById_WithUnknownId_ReturnsNotFound()
    {
        var result = await UpdateAsync(new UpdateProductByIdCommand(Guid.NewGuid(), "x", null, 1f, 1, 1m, []));

        ResultAssert.Failed(result, "Products.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithNameOfAnotherProductOfSameBrand_ReturnsConflict()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateProductByIdCommand(graph.Product.Id, "Butter", null, 1f,
            graph.Unit.Id, 1m, []));

        ResultAssert.Failed(result, "Products.AlreadyExist", ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateById_WithUnknownUnit_ReturnsNotFound()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateProductByIdCommand(graph.Product.Id, "Milk", null, 1f, 999, 1m, []));

        ResultAssert.Failed(result, "Products.UnitNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithNewDescription_UpdatesDescription()
    {
        var graph = await SeededGraph.CreateAsync(database);

        var result = await UpdateAsync(new UpdateProductByIdCommand(graph.Product.Id, "Milk", "Updated", 1.5f,
            graph.Unit.Id, SeededGraph.CurrentPriceValue, []));

        Assert.Equal("Updated", ResultAssert.Succeeded(result).Description);
        var detail = await database.ReadAsync(c => c.ProductDetails.SingleAsync(d => d.ProductId == graph.Product.Id));
        Assert.Equal("Updated", detail.Description);
    }

    [Fact]
    public async Task UpdatePriceById_ClosesCurrentPriceAndAddsNewOne()
    {
        var graph = await SeededGraph.CreateAsync(database);
        await using var context = database.CreateContext();
        var handler = new UpdateProductPriceByIdCommandHandler(context, new FixedClock(),
            NullLogger<UpdateProductPriceByIdCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(
            new UpdateProductPriceByIdCommand(graph.Product.Id, 20m), default));

        Assert.Equal(graph.Product.Id, response.ProductId);
        Assert.Equal(graph.CurrentPrice.Id, response.PreviousPriceId);
        Assert.Equal(20m, response.Value);
        Assert.Equal(FixedClock.Now, response.ValidFrom);

        var previous = await database.ReadAsync(c => c.ProductPrices.SingleAsync(p => p.Id == graph.CurrentPrice.Id));
        var created = await database.ReadAsync(c => c.ProductPrices.SingleAsync(p => p.Id == response.Id));
        Assert.Equal(FixedClock.Now, previous.ValidToTime);
        Assert.Null(created.ValidToTime);
    }

    [Fact]
    public async Task UpdatePriceById_WithUnknownProduct_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new UpdateProductPriceByIdCommandHandler(context, new FixedClock(),
            NullLogger<UpdateProductPriceByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new UpdateProductPriceByIdCommand(Guid.NewGuid(), 20m), default);

        ResultAssert.Failed(result, "Products.NotFound", ErrorType.NotFound);
    }

    private async Task<Result<CreateProductResponse>> CreateAsync(CreateProductCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new CreateProductCommandHandler(context, new FixedClock(),
            NullLogger<CreateProductCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private async Task<PagedResponse<ProductResponse>> GetAsync(GetProductQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetProductQueryHandler(context, NullLogger<GetProductQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<UpdateProductByIdResponse>> UpdateAsync(UpdateProductByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdateProductByIdCommandHandler(context, new FixedClock(),
            NullLogger<UpdateProductByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }
}
