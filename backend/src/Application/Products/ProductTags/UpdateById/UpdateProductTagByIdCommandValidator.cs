using FluentValidation;

namespace Application.Products.ProductTags.UpdateById;

internal sealed class UpdateProductTagByIdCommandValidator
    : AbstractValidator<UpdateProductTagByIdCommand>
{
    public UpdateProductTagByIdCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();
    }
}