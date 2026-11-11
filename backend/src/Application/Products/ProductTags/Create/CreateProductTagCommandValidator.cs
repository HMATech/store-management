using FluentValidation;

namespace Application.Products.ProductTags.Create;

internal sealed class CreateProductTagCommandValidator
    : AbstractValidator<CreateProductTagCommand>
{
    public CreateProductTagCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();
    }
}