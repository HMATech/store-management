using FluentValidation;

namespace Application.Brands.UpdateById;

internal sealed class UpdateBrandByIdCommandValidator
    : AbstractValidator<UpdateBrandByIdCommand>
{
    public UpdateBrandByIdCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();
    }
}