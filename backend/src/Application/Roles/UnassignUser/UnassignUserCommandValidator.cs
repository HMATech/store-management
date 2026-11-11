using FluentValidation;

namespace Application.Roles.UnassignUser;

internal sealed class UnassignUserCommandValidator : AbstractValidator<UnassignUserCommand>
{
    public UnassignUserCommandValidator()
    {
        RuleFor(c => c.User)
            .NotEmpty();
    }
}