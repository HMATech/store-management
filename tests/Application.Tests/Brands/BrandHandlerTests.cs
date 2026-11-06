using Application.Brands.Create;
using Application.Brands.DeleteById;
using Application.Brands.Get;
using Application.Brands.GetById;
using Application.Brands.UpdateById;
using Application.Common;
using Application.Tests.TestSupport;
using Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Tests.Common;

namespace Application.Tests.Brands;

public class BrandHandlerTests
{
    private readonly TestDatabase database = new();

    [Fact]
    public async Task Create_WithNewName_PersistsBrand()
    {
        await using var context = database.CreateContext();
        var handler = new CreateBrandCommandHandler(context, new FixedClock(),
            NullLogger<CreateBrandCommandHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(new CreateBrandCommand("Acme"), default));

        var brand = await database.ReadAsync(c => c.Brands.SingleAsync());
        Assert.Equal(response.Id, brand.Id);
        Assert.Equal("Acme", brand.Name);
        Assert.Equal(FixedClock.Now, brand.CreatedTime);
    }

    [Fact]
    public async Task Create_WithExistingName_ReturnsConflict()
    {
        await database.SeedAsync(Brand.CreateNew("Acme", FixedClock.Now));
        await using var context = database.CreateContext();
        var handler = new CreateBrandCommandHandler(context, new FixedClock(),
            NullLogger<CreateBrandCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateBrandCommand("Acme"), default);

        ResultAssert.Failed(result, "Brands.AlreadyExist", ErrorType.Conflict);
    }

    [Fact]
    public async Task DeleteById_WithExistingBrand_RemovesIt()
    {
        var brand = Brand.CreateNew("Acme", FixedClock.Now);
        await database.SeedAsync(brand);
        await using var context = database.CreateContext();
        var handler = new DeleteBrandByIdCommandHandler(context, NullLogger<DeleteBrandByIdCommandHandler>.Instance);

        ResultAssert.Succeeded(await handler.HandleAsync(new DeleteBrandByIdCommand(brand.Id), default));

        Assert.False(await database.ReadAsync(c => c.Brands.AnyAsync()));
    }

    [Fact]
    public async Task DeleteById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new DeleteBrandByIdCommandHandler(context, NullLogger<DeleteBrandByIdCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteBrandByIdCommand(99), default);

        ResultAssert.Failed(result, "Brands.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task Get_FiltersBySearch()
    {
        await database.SeedAsync(Brand.CreateNew("Acme", FixedClock.Now), Brand.CreateNew("Globex", FixedClock.Now));
        var value = await GetAsync(new GetBrandQuery("Glo", SortOrder.ASC));

        Assert.Equal("Globex", Assert.Single(value.Responses).Name);
    }

    [Theory]
    [InlineData(SortOrder.ASC, new[] { "Acme", "Globex" })]
    [InlineData(SortOrder.DESC, new[] { "Globex", "Acme" })]
    public async Task Get_SortsByName(SortOrder order, string[] expected)
    {
        await database.SeedAsync(Brand.CreateNew("Globex", FixedClock.Now), Brand.CreateNew("Acme", FixedClock.Now));

        var value = await GetAsync(new GetBrandQuery(null, order));

        Assert.Equal(expected, value.Responses.Select(b => b.Name));
    }

    [Fact]
    public async Task Get_PagesResults()
    {
        await database.SeedAsync(Brand.CreateNew("A", FixedClock.Now), Brand.CreateNew("B", FixedClock.Now),
            Brand.CreateNew("C", FixedClock.Now));

        var value = await GetAsync(new GetBrandQuery(null, SortOrder.ASC, Page: 2, PageSize: 2));

        Assert.Equal("C", Assert.Single(value.Responses).Name);
        Assert.Equal(3, value.TotalCount);
        Assert.False(value.HasNextPage);
        Assert.True(value.HasPreviousPage);
    }

    [Fact]
    public async Task GetById_WithExistingBrand_ReturnsIt()
    {
        var brand = Brand.CreateNew("Acme", FixedClock.Now);
        await database.SeedAsync(brand);
        await using var context = database.CreateContext();
        var handler = new GetBrandByIdQueryHandler(context, NullLogger<GetBrandByIdQueryHandler>.Instance);

        var response = ResultAssert.Succeeded(await handler.HandleAsync(new GetBrandByIdQuery(brand.Id), default));

        Assert.Equal(new BrandResponseExpectation(brand.Id, "Acme", FixedClock.Now),
            new BrandResponseExpectation(response.Id, response.Name, response.CreatedTime));
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        await using var context = database.CreateContext();
        var handler = new GetBrandByIdQueryHandler(context, NullLogger<GetBrandByIdQueryHandler>.Instance);

        var result = await handler.HandleAsync(new GetBrandByIdQuery(99), default);

        ResultAssert.Failed(result, "Brands.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithNewName_UpdatesBrand()
    {
        var brand = Brand.CreateNew("Acme", FixedClock.Now);
        await database.SeedAsync(brand);

        var result = await UpdateAsync(new UpdateBrandByIdCommand(brand.Id, "Globex"));

        Assert.Equal("Globex", ResultAssert.Succeeded(result).Name);
        Assert.Equal("Globex", (await database.ReadAsync(c => c.Brands.SingleAsync())).Name);
    }

    [Fact]
    public async Task UpdateById_WithUnknownId_ReturnsNotFound()
    {
        var result = await UpdateAsync(new UpdateBrandByIdCommand(99, "Globex"));

        ResultAssert.Failed(result, "Brands.NotFound", ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateById_WithNameOfAnotherBrand_ReturnsConflict()
    {
        var brand = Brand.CreateNew("Acme", FixedClock.Now);
        await database.SeedAsync(brand, Brand.CreateNew("Globex", FixedClock.Now));

        var result = await UpdateAsync(new UpdateBrandByIdCommand(brand.Id, "Globex"));

        ResultAssert.Failed(result, "Brands.AlreadyExist", ErrorType.Conflict);
    }

    [KnownBugFact("UpdateBrandCommandHandler doesn't exclude the brand itself from its duplicate name check")]
    public async Task UpdateById_WithItsOwnName_Succeeds()
    {
        var brand = Brand.CreateNew("Acme", FixedClock.Now);
        await database.SeedAsync(brand);

        var result = await UpdateAsync(new UpdateBrandByIdCommand(brand.Id, "Acme"));

        Assert.Equal("Acme", ResultAssert.Succeeded(result).Name);
    }

    private async Task<PagedResponse<Application.Brands.BrandResponse>> GetAsync(GetBrandQuery query)
    {
        await using var context = database.CreateContext();
        var handler = new GetBrandQueryHandler(context, NullLogger<GetBrandQueryHandler>.Instance);
        return ResultAssert.Succeeded(await handler.HandleAsync(query, default));
    }

    private async Task<Result<UpdateBrandByIdResponse>> UpdateAsync(UpdateBrandByIdCommand command)
    {
        await using var context = database.CreateContext();
        var handler = new UpdateBrandCommandHandler(context, NullLogger<UpdateBrandCommandHandler>.Instance);
        return await handler.HandleAsync(command, default);
    }

    private sealed record BrandResponseExpectation(int Id, string Name, DateTime CreatedTime);
}
