using FluentValidation;

namespace Application.Stocks.StockAdjustments.Get;

internal sealed class GetStockAdjustmentQueryValidator
    : AbstractValidator<GetStockAdjustmentQuery>
{
    public GetStockAdjustmentQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
