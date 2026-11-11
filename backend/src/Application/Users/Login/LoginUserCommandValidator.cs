using FluentValidation;

namespace Application.Users.Login;

internal sealed class RegisterUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(u => u.Password)
            .NotEmpty();
    }
}