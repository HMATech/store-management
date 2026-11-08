using FluentValidation;

namespace Application.Products;

internal sealed class StockDefinitionCommandValidator
    : AbstractValidator<StockDefinitionCommand>
{
    public StockDefinitionCommandValidator()
    {
        RuleFor(d => d.MinimumQuantity)
            .GreaterThanOrEqualTo(0)
            .When(d => d.MinimumQuantity.HasValue);
    }
}
