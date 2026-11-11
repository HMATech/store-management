using FluentValidation;

namespace Application.Roles.AssignUser;

internal sealed class AssignUserCommandValidator
    : AbstractValidator<AssignUserCommand>
{
    public AssignUserCommandValidator()
    {
        RuleFor(c => c.User)
            .NotEmpty();
    }
}