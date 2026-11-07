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
    }
}
