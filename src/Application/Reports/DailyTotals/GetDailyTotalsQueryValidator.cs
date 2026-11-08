using FluentValidation;

namespace Application.Reports.DailyTotals;

internal sealed class GetDailyTotalsQueryValidator
    : AbstractValidator<GetDailyTotalsQuery>
{
    public const int MaxDays = 366;

    public GetDailyTotalsQueryValidator()
    {
        RuleFor(q => q.From)
            .NotEmpty();

        RuleFor(q => q.To)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .GreaterThanOrEqualTo(q => q.From)
                .WithMessage("'To' must be on or after 'From'")
            .Must((q, to) => to.DayNumber - q.From.DayNumber < MaxDays)
                .WithMessage($"The range can cover at most {MaxDays} days");

        RuleFor(q => q.TimeZone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(id => TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
                .WithMessage("'{PropertyValue}' isn't a known time zone");
    }
}
