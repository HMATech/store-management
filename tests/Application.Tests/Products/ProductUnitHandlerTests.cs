using Application.Common;
using Application.Products.ProductUnits;
using Application.Products.ProductUnits.Create;
using Application.Products.ProductUnits.DeleteById;
using Application.Products.ProductUnits.Get;
using Application.Products.ProductUnits.GetById;
using Application.Products.ProductUnits.UpdateById;
using Application.Tests.TestSupport;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Tests.Common;

namespace Application.Tests.Products;

public class ProductUnitHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task Create_WithNewName_PersistsUnit()
    {
        await using var context = database.CreateContext();
        var handler = new CreateProductUnitCommandHandler(context, new FixedClock(),
            NullLogger<CreateProductUnitCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(
            await handler.HandleAsync(new CreateProductUnitCommand("Litre"), default));

        Assert.Equal("Litre", response.Name);
        Assert.Equal("litre", response.NormalizedName);
        Assert.Equal(response.Id, (await database.ReadAsync(c => c.ProductUnits.SingleAsync())).Id);
    }

    [Theory]
    [InlineData("Litre")]
    [InlineData("  LITRE ")]
    public async Task Create_WithSameNormalizedName_ReturnsConflict(string name)
    {
        await database.SeedAsync(ProductUnit.CreateNew("Litre"));
        await using var context = database.CreateContext();
        var handler = new CreateProductUnitCommandHandler(context, new FixedClock(),
            NullLogger<CreateProductUnitCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateProductUnitCommand(name), default);

        ResultAssert.Failed(result, "Products.UnitAlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteById_WithExistingUnit_RemovesIt()
    {
        var unit = ProductUnit.CreateNew("Litre");
        await database.SeedAsync(unit);
        await using var context = database.CreateContext();
        var handler = new DeleteProductUnitByIdCommandHandler(context,
            NullLogger<DeleteProductUnitByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeleteProductUnitByIdCommand(unit.Id), default));

        Assert.False(await database.ReadAsync(c => c.ProductUnits.AnyAsync()));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteProductUnitByIdCommandHandler(context,
            NullLogger<DeleteProductUnitByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteProductUnitByIdCommand(99), default);

        ResultAssert.Failed(result, "Products.UnitNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_FiltersBySearch()
    {
        await database.SeedAsync(ProductUnit.CreateNew("Litre"), ProductUnit.CreateNew("Gram"));

        var value = await GetAsync(new GetProductUnitQuery("Gr", SortOrder.ASC));

        Assert.Equal("Gram", Assert.Single(value).Name);
    }

    [Theory]
    [InlineData(SortOrder.ASC, new[] { "Gram", "Litre" })]
    [InlineData(SortOrder.DESC, new[] { "Litre", "Gram" })]
    public async Task Get_SortsByName(SortOrder order, string[] expected)
    {
        await database.SeedAsync(ProductUnit.CreateNew("Litre"), ProductUnit.CreateNew("Gram"));

        var value = await GetAsync(new GetProductUnitQuery(null, order));

        Assert.Equal(expected, value.Select(u => u.Name));
    }

    [Fact]
    public async Task GetById_WithExistingUnit_ReturnsIt()
    {
        var unit = ProductUnit.CreateNew("Litre");
        await database.SeedAsync(unit);
        await using var context = database.CreateContext();
        var handler = new GetProductUnitByIdQueryHandler(context, NullLogger<GetProductUnitByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(new GetProductUnitByIdQuery(unit.Id), default));

        Assert.Equal(new ProductUnitResponse(unit.Id, "Litre", "litre"), response);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetProductUnitByIdQueryHandler(context, NullLogger<GetProductUnitByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetProductUnitByIdQuery(99), default);

        ResultAssert.Failed(result, "Products.UnitNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithNewName_UpdatesUnit()
    {
        var unit = ProductUnit.CreateNew("Litre");
        await database.SeedAsync(unit);

        var result = await UpdateAsync(new UpdateProductUnitByIdCommand(unit.Id, "Gram"));

        Assert.Equal(new UpdateProductUnitByIdResponse(unit.Id, "Gram", "gram"), ResultAssert.Succeeded(result));
        Assert.Equal("Gram", (await database.ReadAsync(c => c.ProductUnits.SingleAsync())).Name.Value);
    }

    [Fact]
    public async Task UpdateById_WithIdenticalName_ReturnsUnitUnchanged()
    {
        var unit = ProductUnit.CreateNew("Litre");
        await database.SeedAsync(unit);

        var result = await UpdateAsync(new UpdateProductUnitByIdCommand(unit.Id, "Litre"));

        Assert.Equal("Litre", ResultAssert.Succeeded(result).Name);
    }

    [KnownBugFact("UpdateProductUnitByIdCommandHandler returns early on a case-only rename, so it's never saved")]
    public async Task UpdateById_WithSameNameInDifferentCase_UpdatesUnit()
    {
        var unit = ProductUnit.CreateNew("Litre");
        await database.SeedAsync(unit);

        var result = await UpdateAsync(new UpdateProductUnitByIdCommand(unit.Id, "LITRE"));

        Assert.Equal("LITRE", ResultAssert.Succeeded(result).Name);
    }

    [Fact]
    public async Task UpdateById_WithNameOfAnotherUnit_ReturnsConflict()
    {
        var unit = ProductUnit.CreateNew("Litre");
        await database.SeedAsync(unit, ProductUnit.CreateNew("Gram"));

        var result = await UpdateAsync(new UpdateProductUnitByIdCommand(unit.Id, "gram"));

        ResultAssert.Failed(result, "Products.UnitAlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateById_WithUnknownId_ReturnsNotFound()
    {
        var result = await UpdateAsync(new UpdateProductUnitByIdCommand(99, "Gram"));

        ResultAssert.Failed(result, "Products.UnitNotFound", ErrorType.NotFound);
    }

    private async Task<List<ProductUnitResponse>> GetAsync(GetProductUnitQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetProductUnitQueryHandler(context, NullLogger<GetProductUnitQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<UpdateProductUnitByIdResponse>> UpdateAsync(UpdateProductUnitByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdateProductUnitByIdCommandHandler(context,
            NullLogger<UpdateProductUnitByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }
}
