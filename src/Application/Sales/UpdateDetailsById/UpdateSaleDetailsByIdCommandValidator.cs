using FluentValidation;
using Application.Common;

namespace Application.Sales.UpdateDetailsById;

internal sealed class UpdateSaleDetailsByIdCommandValidator
    : AbstractValidator<UpdateSaleDetailsByIdCommand>
{
    public UpdateSaleDetailsByIdCommandValidator()
    {
        // A sale's title is optional, so it may be empty but not missing.
        RuleFor(c => c.Title)
            .NotNull();

        RuleFor(c => c.Tags)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MustNotHaveAnyEmptyString();

        RuleFor(c => c.OccurrenceTime)
            .NotEmpty();
    }
}