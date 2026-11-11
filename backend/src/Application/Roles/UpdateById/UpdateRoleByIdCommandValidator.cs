using FluentValidation;

namespace Application.Roles.UpdateById;

internal sealed class UpdateRoleByIdCommandValidator
    : AbstractValidator<UpdateRoleByIdCommand>
{
    public UpdateRoleByIdCommandValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty()
            .MinimumLength(3);
    }
}