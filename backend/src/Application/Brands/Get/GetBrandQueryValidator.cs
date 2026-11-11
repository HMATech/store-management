using FluentValidation;

namespace Application.Brands.Get;

internal sealed class GetBrandQueryValidator
    : AbstractValidator<GetBrandQuery>
{
    public GetBrandQueryValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(q => q.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
