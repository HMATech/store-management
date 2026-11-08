using FluentValidation;

namespace Application.Stocks.Get;

internal sealed class GetStockQueryValidator
    : AbstractValidator<GetStockQuery>
{
    public GetStockQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.Status)
            .Must(s => StockQueries.TryParseStatus(s, out _))
                .WithMessage($"'{{PropertyValue}}' isn't a stock status; use one of: " +
                    string.Join(", ", Enum.GetNames<StockStatus>()))
            .When(q => !string.IsNullOrEmpty(q.Status));
    }
}
