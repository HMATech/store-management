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
    }
}
