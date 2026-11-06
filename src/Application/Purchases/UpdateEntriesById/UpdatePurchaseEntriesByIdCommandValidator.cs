using FluentValidation;
using Application.Common;

namespace Application.Purchases.UpdateEntriesById;

internal sealed class UpdatePurchaseEntriesByIdCommandValidator
    : AbstractValidator<UpdatePurchaseEntriesByIdCommand>
{
    public UpdatePurchaseEntriesByIdCommandValidator()
    {
        RuleFor(c => c.ProductEntries)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MustNotHaveAnyNullElement()
            .Must((_, entries, ctx) =>
            {
                bool isValid = true;

                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry.ProductId == Guid.Empty)
                    {
                        ctx.AddFailure($"Product entry's 'ProductId' at index {i} should not be empty");
                        isValid = false;
                    }

                    if (entry.Quantity < 1)
                    {
                        ctx.AddFailure($"Product entry's 'Amount' at index {i} for product id '{entry.Id}' " +
                            "should be at least 1");
                        isValid = false;
                    }
                }

                return isValid;
            })
                .WithErrorCode("ProductEntriesValidator")
                .WithMessage("Invalid Product Entries");
    }
}