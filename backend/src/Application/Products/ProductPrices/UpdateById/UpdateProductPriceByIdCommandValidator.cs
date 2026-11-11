using FluentValidation;

namespace Application.Products.ProductPrices.UpdateById;

internal sealed class UpdateProductPriceByIdCommandValidator
    : AbstractValidator<UpdateProductPriceByIdCommand>
{
    public UpdateProductPriceByIdCommandValidator()
    {
        RuleFor(c => c.Value)
            .GreaterThanOrEqualTo(0);
    }
}