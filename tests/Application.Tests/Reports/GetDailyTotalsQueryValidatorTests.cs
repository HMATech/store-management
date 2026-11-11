using Application.Reports.DailyTotals;

namespace Application.Tests.Reports;

public class GetDailyTotalsQueryValidatorTests
{
    private static readonly DateOnly Day = new(2026, 1, 10);

    private readonly GetDailyTotalsQueryValidator validator = new();

    [Theory]
    [InlineData("UTC")]
    [InlineData("Asia/Jakarta")]
    public void Validate_WithValidQuery_Passes(string timeZone)
    {
        Assert.True(validator.Validate(new GetDailyTotalsQuery(Day, Day.AddDays(6), timeZone)).IsValid);
    }

    [Fact]
    public void Validate_WithTheLongestAllowedRange_Passes()
    {
        var query = new GetDailyTotalsQuery(Day, Day.AddDays(GetDailyTotalsQueryValidator.MaxDays - 1), "UTC");

        Assert.True(validator.Validate(query).IsValid);
    }

    public static TheoryData<GetDailyTotalsQuery, string> InvalidQueries => new()
    {
        { new GetDailyTotalsQuery(default, Day, "UTC"), "From" },
        { new GetDailyTotalsQuery(Day, default, "UTC"), "To" },
        { new GetDailyTotalsQuery(Day, Day.AddDays(-1), "UTC"), "To" },
        { new GetDailyTotalsQuery(Day, Day.AddDays(GetDailyTotalsQueryValidator.MaxDays), "UTC"), "To" },
        { new GetDailyTotalsQuery(Day, Day, ""), "TimeZone" },
        { new GetDailyTotalsQuery(Day, Day, "Mars/Olympus"), "TimeZone" },
    };

    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public void Validate_WithInvalidField_ReportsThatField(GetDailyTotalsQuery query, string field)
    {
        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == field);
    }
}
