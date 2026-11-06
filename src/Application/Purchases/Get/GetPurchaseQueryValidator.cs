using FluentValidation;

namespace Application.Purchases.Get;

internal sealed class GetPurchaseQueryValidator
    : AbstractValidator<GetPurchaseQuery>
{
    public GetPurchaseQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
