using FluentValidation;
using Application.Common;

namespace Application.Products.UpdatePriceById;

internal sealed class UpdateProductPriceByIdCommandValidator
    : AbstractValidator<UpdateProductPriceByIdCommand>
{
    public UpdateProductPriceByIdCommandValidator()
    {
        RuleFor(c => c.Value)
            .GreaterThanOrEqualTo(0);
    }
}