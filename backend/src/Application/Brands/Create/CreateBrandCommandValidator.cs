using FluentValidation;

namespace Application.Brands.Create;

internal sealed class CreateBrandCommandValidator
    : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();
    }
}