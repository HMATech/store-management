using FluentValidation;

namespace Application.Users.UpdateDetailById;

internal sealed class UpdateUserDetailByIdCommandValidator : AbstractValidator<UpdateUserDetailByIdCommand>
{
    public UpdateUserDetailByIdCommandValidator()
    {
        RuleFor(u => u.FirstName)
            .NotEmpty()
            .MinimumLength(3);
    }
}