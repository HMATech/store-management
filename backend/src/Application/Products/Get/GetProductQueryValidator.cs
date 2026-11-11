using Application.Stocks;
using FluentValidation;

namespace Application.Products.Get;

internal sealed class GetProductQueryValidator
    : AbstractValidator<GetProductQuery>
{
    public GetProductQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.StockStatus)
            .Must(s => StockQueries.TryParseStatus(s, out _))
                .WithMessage($"'{{PropertyValue}}' isn't a stock status; use one of: " +
                    string.Join(", ", Enum.GetNames<StockStatus>()))
            .When(q => !string.IsNullOrEmpty(q.StockStatus));
    }
}
