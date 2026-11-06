using FluentValidation;

namespace Application.Sales.Get;

internal sealed class GetSaleQueryValidator
    : AbstractValidator<GetSaleQuery>
{
    public GetSaleQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
