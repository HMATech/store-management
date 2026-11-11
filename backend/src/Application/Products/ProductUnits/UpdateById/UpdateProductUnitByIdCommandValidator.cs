using FluentValidation;

namespace Application.Products.ProductUnits.UpdateById;

internal sealed class UpdateProductUnitByIdCommandValidator
    : AbstractValidator<UpdateProductUnitByIdCommand>
{
    public UpdateProductUnitByIdCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();
    }
}