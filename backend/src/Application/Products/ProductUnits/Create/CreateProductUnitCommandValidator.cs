using FluentValidation;

namespace Application.Products.ProductUnits.Create;

internal sealed class CreateProductUnitCommandValidator
    : AbstractValidator<CreateProductUnitCommand>
{
    public CreateProductUnitCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();
    }
}