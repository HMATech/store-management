using FluentValidation;

namespace Application.Transactions.Get;

internal sealed class GetTransactionQueryValidator
    : AbstractValidator<GetTransactionQuery>
{
    public GetTransactionQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.Kind)
            .Must(k => GetTransactionQueryHandler.TryParseKind(k, out _))
                .WithMessage("'{PropertyValue}' isn't a transaction kind; use one of: " +
                    string.Join(", ", Enum.GetNames<TransactionKind>()))
            .When(q => !string.IsNullOrEmpty(q.Kind));

        RuleFor(q => q.To)
            .GreaterThanOrEqualTo(q => q.From)
                .WithMessage("'To' must be on or after 'From'")
            .When(q => q.From.HasValue && q.To.HasValue);

        RuleFor(q => q.TimeZone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(id => TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
                .WithMessage("'{PropertyValue}' isn't a known time zone");
    }
}
