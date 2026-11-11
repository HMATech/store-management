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

                    if (entry.UnitCost < 0)
                    {
                        ctx.AddFailure($"Product entry's 'UnitCost' at index {i} for product id '{entry.Id}' " +
                            "should not be negative");
                        isValid = false;
                    }
                }

                var conflictingEntryIds = entries
                    .Where(e => e.Id.HasValue)
                    .GroupBy(e => e.Id!.Value)
                    .Where(g => g.Select(e => e.UnitCost).Distinct().Count() > 1)
                    .Select(g => g.Key);
                foreach (var entryId in conflictingEntryIds)
                {
                    ctx.AddFailure($"Product entry with id '{entryId}' is listed more than once " +
                        "with different unit costs");
                    isValid = false;
                }

                if (entries.Sum(e => (long)e.Quantity) > int.MaxValue)
                {
                    ctx.AddFailure("The total quantity of product entries is too large");
                    isValid = false;
                }

                return isValid;
            })
                .WithErrorCode("ProductEntriesValidator")
                .WithMessage("Invalid Product Entries");
    }
}