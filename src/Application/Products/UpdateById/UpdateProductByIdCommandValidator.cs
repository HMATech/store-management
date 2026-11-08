using FluentValidation;
using Application.Common;

namespace Application.Products.UpdateById;

internal sealed class UpdateProductByIdCommandValidator
    : AbstractValidator<UpdateProductByIdCommand>
{
    public UpdateProductByIdCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty();

        RuleFor(c => c.Id)
            .NotEmpty();

        RuleFor(c => c.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.Measure)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.UnitId)
            .NotEmpty();

        RuleFor(c => c.Tags)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MustNotHaveAnyEmptyString();

        RuleFor(c => c.StockDefinition!)
            .SetValidator(new StockDefinitionCommandValidator())
            .When(c => c.StockDefinition != null);
    }
}