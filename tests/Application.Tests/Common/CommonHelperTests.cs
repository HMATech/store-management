using Application.Common;
using Application.Tests.TestSupport;
using Domain.Products;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Application.Tests.Common;

public class CommonHelperTests
{
    private readonly TestDatabase database = new();

    [Theory]
    [InlineData(1, 2, 5, true, false)]
    [InlineData(2, 2, 5, true, true)]
    [InlineData(3, 2, 5, false, true)]
    [InlineData(1, 2, 2, false, false)]
    [InlineData(1, 10, 0, false, false)]
    public void PagedResponse_ComputesPageFlags(int page, int pageSize, int total, bool hasNext, bool hasPrevious)
    {
        var response = PagedResponse<int>.Create(page, pageSize, total, []);

        Assert.Equal(hasNext, response.HasNextPage);
        Assert.Equal(hasPrevious, response.HasPreviousPage);
    }

    [Fact]
    public async Task PagedResponse_PageBeyondEnd_IsEmptyButKeepsTotal()
    {
        await database.SeedAsync(ProductTag.CreateNew("A"), ProductTag.CreateNew("B"));
        await using var context = database.CreateContext();
        var query = context.ProductTags.OrderBy(t => t.Id).Select(t => t.Id);

        var response = await PagedResponse<int>.CreateAsync(query, page: 5, pageSize: 10);

        Assert.Empty(response.Responses);
        Assert.Equal(2, response.TotalCount);
        Assert.False(response.HasNextPage);
    }

    [Fact]
    public async Task SynchronizedTags_ForEntityWithoutTags_CreatesNewTags()
    {
        await using var context = database.CreateContext();

        var tags = await SyncAsync(context, ["Dairy", "Frozen"], []);

        Assert.Equal(["dairy", "frozen"], tags.Select(t => t.Name.Normalized).Order());
    }

    [Fact]
    public async Task SynchronizedTags_ReusesTagFromDatabaseIgnoringCase()
    {
        var existing = ProductTag.CreateNew("Dairy");
        await database.SeedAsync(existing);
        await using var context = database.CreateContext();

        var tags = await SyncAsync(context, ["  DAIRY "], []);

        Assert.Equal(existing.Id, Assert.Single(tags).Id);
    }

    [Fact]
    public async Task SynchronizedTags_KeepsEntityTagsStillRequested_AndDropsOthers()
    {
        var keep = ProductTag.CreateNew("Keep");
        var drop = ProductTag.CreateNew("Drop");
        await database.SeedAsync(keep, drop);
        await using var context = database.CreateContext();
        var entityTags = await context.ProductTags.ToListAsync();

        var tags = await SyncAsync(context, ["KEEP", "New"], entityTags);

        Assert.Equal(["keep", "new"], tags.Select(t => t.Name.Normalized).Order());
    }

    [Fact]
    public async Task SynchronizedTags_WithEmptyInput_RemovesAllTags()
    {
        await database.SeedAsync(ProductTag.CreateNew("Drop"));
        await using var context = database.CreateContext();

        var tags = await SyncAsync(context, [], await context.ProductTags.ToListAsync());

        Assert.Empty(tags);
    }

    [Fact]
    public async Task SynchronizedTags_WithDuplicateInput_ReturnsOneTag()
    {
        await using var context = database.CreateContext();

        var tags = await SyncAsync(context, ["Dairy", "Dairy", "DAIRY"], []);

        Assert.Single(tags);
    }

    [Fact]
    public async Task SynchronizedTags_WhenCancelled_Throws()
    {
        await using var context = database.CreateContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TagsHelper.CreateSynchronizedTagsAsync(["Dairy"],
            ProductTag.CreateNew, [], context.ProductTags, cancellation.Token));
    }

    [Fact]
    public void ApplicationErrors_BuildCodesFromLayerAndTitle()
    {
        var inner = Error.Problem("Inner", "Inner detail");

        AssertSystemError(ApplicationErrors.ApplicationError("Part", inner), "Part.ApplicationError:Inner");
        AssertSystemError(ApplicationErrors.ApplicationError("Part", "Message"), "Part.ApplicationError");
        AssertSystemError(ApplicationErrors.DBOperationError("Part", "Message"), "Part.DBOperationError");
        AssertSystemError(ApplicationErrors.OperationCancelledError("Part", "Message"), "Part.OperationError");
        AssertSystemError(ApplicationErrors.UnexpectedError("Part", "Message"), "Part.UnexpectedError");
        Assert.Contains("Inner detail", ApplicationErrors.ApplicationError("Part", inner).Description);
    }

    [Theory]
    [InlineData(new[] { "a", "b" }, true)]
    [InlineData(new string[0], true)]
    [InlineData(new[] { "a", "" }, false)]
    [InlineData(new[] { "a", null }, false)]
    public void MustNotHaveAnyEmptyString_ChecksEveryElement(string?[] values, bool expected)
    {
        var result = new ArrayValidator().Validate(new ArrayHolder(values!, []));

        Assert.Equal(expected, result.IsValid);
        if (!expected)
        {
            Assert.Equal("StringArrayWithNoEmptyElementValidator", Assert.Single(result.Errors).ErrorCode);
        }
    }

    [Fact]
    public void MustNotHaveAnyNullElement_RejectsNullElement()
    {
        var result = new ArrayValidator().Validate(new ArrayHolder([], ["a", null!]));

        Assert.Equal("ListWithNoEmptyElementValidator", Assert.Single(result.Errors).ErrorCode);
    }

    [Fact]
    public void MustNotHaveAnyNullElement_AcceptsListWithoutNulls()
    {
        Assert.True(new ArrayValidator().Validate(new ArrayHolder([], ["a"])).IsValid);
    }

    private static async Task<List<ProductTag>> SyncAsync(Infrastructure.Database.ApplicationDbContext context,
        string[] input, IEnumerable<ProductTag> entityTags)
    {
        var tags = await TagsHelper.CreateSynchronizedTagsAsync(input, ProductTag.CreateNew, entityTags,
            context.ProductTags, default);
        return tags.ToList();
    }

    private static void AssertSystemError(Error error, string expectedCode)
    {
        Assert.Equal(expectedCode, error.Code);
        Assert.Equal(ErrorType.System, error.ErrorType);
    }

    private sealed record ArrayHolder(string[] Strings, IList<string> Items);

    private sealed class ArrayValidator : AbstractValidator<ArrayHolder>
    {
        public ArrayValidator()
        {
            RuleFor(h => h.Strings).MustNotHaveAnyEmptyString();
            RuleFor(h => h.Items).MustNotHaveAnyNullElement();
        }
    }
}
