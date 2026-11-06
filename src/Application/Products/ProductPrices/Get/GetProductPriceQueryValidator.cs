using FluentValidation;

namespace Application.Products.ProductPrices.Get;

internal sealed class GetProductPriceQueryValidator
    : AbstractValidator<GetProductPriceQuery>
{
    public GetProductPriceQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
