using FluentValidation;
using Application.Common;

namespace Application.Sales.UpdateDetailsById;

internal sealed class UpdateSaleDetailsByIdCommandValidator
    : AbstractValidator<UpdateSaleDetailsByIdCommand>
{
    public UpdateSaleDetailsByIdCommandValidator()
    {
        RuleFor(c => c.Title)
            .NotEmpty();

        RuleFor(c => c.Tags)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .MustNotHaveAnyEmptyString();

        RuleFor(c => c.OccurrenceTime)
            .NotEmpty();
    }
}