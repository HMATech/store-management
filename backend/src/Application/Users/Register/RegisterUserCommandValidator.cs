using static Shared.PasswordRegex;
using FluentValidation;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(u => u.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(PasswordOneLowercaseRequirementRegex())
                .WithMessage("'Password' must have at least one lowercase character.")
            .Matches(PasswordOneUppercaseRequirementRegex())
                .WithMessage("'Password' must have at least one uppercase character.")
            .Matches(PasswordOneNumberRequirementRegex())
                .WithMessage("'Password' must have at least one number.")
            .Matches(PasswordOneSpecialCharacterRequirementRegex())
                .WithMessage("'Password' must have at least one special character (#?!@$%^&*-).");

        RuleFor(u => u.FirstName)
            .NotEmpty()
            .MinimumLength(3);
    }
}