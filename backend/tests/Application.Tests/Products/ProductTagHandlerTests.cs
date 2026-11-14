using Application.Common;
using Application.Products.ProductTags;
using Application.Products.ProductTags.Create;
using Application.Products.ProductTags.DeleteById;
using Application.Products.ProductTags.Get;
using Application.Products.ProductTags.GetById;
using Application.Products.ProductTags.UpdateById;
using Application.Tests.TestSupport;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;

namespace Application.Tests.Products;

public class ProductTagHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task Create_WithNewName_PersistsTag()
    {
        await using var context = database.CreateContext();
        var handler = new CreateProductTagCommandHandler(context, new FixedClock(),
            NullLogger<CreateProductTagCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(new CreateProductTagCommand("Dairy"), default));

        Assert.Equal("Dairy", response.Name);
        Assert.Equal("dairy", response.NormalizedName);
        Assert.Equal(response.Id, (await database.ReadAsync(c => c.ProductTags.SingleAsync())).Id);
    }

    [Theory]
    [InlineData("Dairy")]
    [InlineData("  DAIRY ")]
    public async Task Create_WithSameNormalizedName_ReturnsConflict(string name)
    {
        await database.SeedAsync(ProductTag.CreateNew("Dairy"));
        await using var context = database.CreateContext();
        var handler = new CreateProductTagCommandHandler(context, new FixedClock(),
            NullLogger<CreateProductTagCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateProductTagCommand(name), default);

        ResultAssert.Failed(result, "Products.TagAlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteById_WithExistingTag_RemovesIt()
    {
        var tag = ProductTag.CreateNew("Dairy");
        await database.SeedAsync(tag);
        await using var context = database.CreateContext();
        var handler = new DeleteProductTagByIdCommandHandler(context,
            NullLogger<DeleteProductTagByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeleteProductTagByIdCommand(tag.Id), default));

        Assert.False(await database.ReadAsync(c => c.ProductTags.AnyAsync()));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteProductTagByIdCommandHandler(context,
            NullLogger<DeleteProductTagByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteProductTagByIdCommand(99), default);

        ResultAssert.Failed(result, "Products.TagNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_FiltersBySearch()
    {
        await database.SeedAsync(ProductTag.CreateNew("Dairy"), ProductTag.CreateNew("Frozen"));

        var value = await GetAsync(new GetProductTagQuery("Fro", SortOrder.ASC));

        Assert.Equal("Frozen", Assert.Single(value).Name);
    }

    [Theory]
    [InlineData("fro")]
    [InlineData("FROZEN")]
    [InlineData("  fRo  ")]
    public async Task Get_SearchIgnoresLetterCaseAndOuterSpaces(string search)
    {
        await database.SeedAsync(ProductTag.CreateNew("Dairy"), ProductTag.CreateNew("Frozen"));

        var value = await GetAsync(new GetProductTagQuery(search, SortOrder.ASC));

        Assert.Equal("Frozen", Assert.Single(value).Name);
    }

    [Theory]
    [InlineData(SortOrder.ASC, new[] { "Dairy", "Frozen" })]
    [InlineData(SortOrder.DESC, new[] { "Frozen", "Dairy" })]
    public async Task Get_SortsByName(SortOrder order, string[] expected)
    {
        await database.SeedAsync(ProductTag.CreateNew("Frozen"), ProductTag.CreateNew("Dairy"));

        var value = await GetAsync(new GetProductTagQuery(null, order));

        Assert.Equal(expected, value.Select(t => t.Name));
    }

    [Fact]
    public async Task GetById_WithExistingTag_ReturnsIt()
    {
        var tag = ProductTag.CreateNew("Dairy");
        await database.SeedAsync(tag);
        await using var context = database.CreateContext();
        var handler = new GetProductTagByIdQueryHandler(context, NullLogger<GetProductTagByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(new GetProductTagByIdQuery(tag.Id), default));

        Assert.Equal(new ProductTagResponse(tag.Id, "Dairy", "dairy"), response);
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetProductTagByIdQueryHandler(context, NullLogger<GetProductTagByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetProductTagByIdQuery(99), default);

        ResultAssert.Failed(result, "Products.TagNotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithNewName_UpdatesTag()
    {
        var tag = ProductTag.CreateNew("Dairy");
        await database.SeedAsync(tag);

        var result = await UpdateAsync(new UpdateProductTagByIdCommand(tag.Id, "Frozen"));

        Assert.Equal(new UpdateProductTagByIdResponse(tag.Id, "Frozen", "frozen"), ResultAssert.Succeeded(result));
        Assert.Equal("Frozen", (await database.ReadAsync(c => c.ProductTags.SingleAsync())).Name.Value);
    }

    [Fact]
    public async Task UpdateById_WithSameNameInDifferentCase_UpdatesTag()
    {
        var tag = ProductTag.CreateNew("Dairy");
        await database.SeedAsync(tag);

        var result = await UpdateAsync(new UpdateProductTagByIdCommand(tag.Id, "DAIRY"));

        Assert.Equal("DAIRY", ResultAssert.Succeeded(result).Name);
        Assert.Equal("DAIRY", (await database.ReadAsync(c => c.ProductTags.SingleAsync())).Name.Value);
    }

    [Fact]
    public async Task UpdateById_WithIdenticalName_ReturnsTagUnchanged()
    {
        var tag = ProductTag.CreateNew("Dairy");
        await database.SeedAsync(tag);

        var result = await UpdateAsync(new UpdateProductTagByIdCommand(tag.Id, "Dairy"));

        Assert.Equal("Dairy", ResultAssert.Succeeded(result).Name);
    }

    [Fact]
    public async Task UpdateById_WithNameOfAnotherTag_ReturnsConflict()
    {
        var tag = ProductTag.CreateNew("Dairy");
        await database.SeedAsync(tag, ProductTag.CreateNew("Frozen"));

        var result = await UpdateAsync(new UpdateProductTagByIdCommand(tag.Id, "frozen"));

        ResultAssert.Failed(result, "Products.TagAlreadyExists", ErrorType.Conflict);
    }

    [Fact]
    public async Task UpdateById_WithUnknownId_ReturnsNotFound()
    {
        var result = await UpdateAsync(new UpdateProductTagByIdCommand(99, "Frozen"));

        ResultAssert.Failed(result, "Products.TagNotFound", ErrorType.NotFound);
    }

    private async Task<List<ProductTagResponse>> GetAsync(GetProductTagQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetProductTagQueryHandler(context, NullLogger<GetProductTagQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<UpdateProductTagByIdResponse>> UpdateAsync(UpdateProductTagByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdateProductTagByIdCommandHandler(context,
            NullLogger<UpdateProductTagByIdCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }
}
