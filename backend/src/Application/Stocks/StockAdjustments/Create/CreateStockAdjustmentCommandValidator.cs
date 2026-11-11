using FluentValidation;

namespace Application.Stocks.StockAdjustments.Create;

internal sealed class CreateStockAdjustmentCommandValidator
    : AbstractValidator<CreateStockAdjustmentCommand>
{
    public CreateStockAdjustmentCommandValidator()
    {
        RuleFor(c => c.ProductId)
            .NotEmpty();

        RuleFor(c => c.Quantity)
            .NotEqual(0)
                .WithMessage("'Quantity' must not be 0. Use a positive number to add stock and a negative one " +
                    "to remove it.");

        RuleFor(c => c.Reason)
            .NotEmpty();

        RuleFor(c => c.OccurrenceTime)
            .NotEmpty();
    }
}
