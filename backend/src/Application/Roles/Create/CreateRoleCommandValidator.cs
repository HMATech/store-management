using FluentValidation;

namespace Application.Roles.Create;

internal sealed class CreateRoleCommandValidator
    : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(c => c.Role)
            .NotEmpty()
            .MinimumLength(3);
    }
}