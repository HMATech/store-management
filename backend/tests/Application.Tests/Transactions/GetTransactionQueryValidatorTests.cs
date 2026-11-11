using Application.Common;
using Application.Transactions.Get;

namespace Application.Tests.Transactions;

public class GetTransactionQueryValidatorTests
{
    private static readonly DateOnly Day = new(2026, 1, 10);

    private readonly GetTransactionQueryValidator validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("sale")]
    [InlineData("Purchase")]
    public void Validate_WithValidQuery_Passes(string? kind)
    {
        Assert.True(validator.Validate(new GetTransactionQuery(SortOrder.DESC, kind, Day, Day, "Asia/Jakarta"))
            .IsValid);
    }

    [Fact]
    public void Validate_WithoutDates_Passes()
    {
        Assert.True(validator.Validate(new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC")).IsValid);
    }

    public static TheoryData<GetTransactionQuery, string> InvalidQueries => new()
    {
        { new GetTransactionQuery(SortOrder.DESC, "refund", null, null, "UTC"), "Kind" },
        { new GetTransactionQuery(SortOrder.DESC, "1", null, null, "UTC"), "Kind" },
        { new GetTransactionQuery(SortOrder.DESC, null, Day, Day.AddDays(-1), "UTC"), "To" },
        { new GetTransactionQuery(SortOrder.DESC, null, null, null, ""), "TimeZone" },
        { new GetTransactionQuery(SortOrder.DESC, null, null, null, "Mars/Olympus"), "TimeZone" },
        { new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC", Page: 0), "Page" },
        { new GetTransactionQuery(SortOrder.DESC, null, null, null, "UTC", PageSize: 0), "PageSize" },
    };

    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public void Validate_WithInvalidField_ReportsThatField(GetTransactionQuery query, string field)
    {
        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == field);
    }
}
