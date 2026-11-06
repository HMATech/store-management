using FluentValidation;
using Application.Common;

namespace Application.Products.Create;

internal sealed class CreateProductCommandValidator
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();

        RuleFor(c => c.BrandId)
            .NotEmpty()
            .GreaterThan(0);

        RuleFor(c => c.UnitId)
            .NotEmpty()
            .GreaterThan(0);

        RuleFor(c => c.Measure)
            .GreaterThanOrEqualTo(0f);

        RuleFor(c => c.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.Tags)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MustNotHaveAnyEmptyString();
    }
}