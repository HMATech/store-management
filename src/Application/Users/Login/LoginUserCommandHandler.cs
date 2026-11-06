using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Users.Login;

internal sealed class LoginUserCommandHandler(IPasswordHasher passwordHasher,
    IApplicationDbContext dbContext,
    ITokenProvider tokenProvider,
    ILogger<LoginUserCommandHandler> logger) 
        : ICommandHandler<LoginUserCommand, LoginResponse>
{
    public async Task<Result<LoginResponse>> HandleAsync(LoginUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

            if (user == null)
            {
                return UserErrors.IncorrectCredential();
            }

            var passwordValidation = passwordHasher.Verify(command.Password, user.PasswordHash);

            if (passwordValidation.IsFailed)
            {
                if (passwordValidation.Error.Code == IPasswordHasherExtensions.IncorrectPasswordCode)
                {
                    return UserErrors.IncorrectCredential();
                }

                logger.LogWarning("Application error has occurred while verifying user '{command.Email}' 's credential",
                    command.Email);

                return ApplicationErrors.ApplicationError(nameof(Users), passwordValidation.Error);
            }

            return new LoginResponse(user.Id, tokenProvider.CreateFor(user));
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(LoginUserCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while logging in user '{command.Email}'",
                command.Email);
            return ApplicationErrors.UnexpectedError(nameof(LoginUserCommandHandler),
                $"Unexpected error has occurred while logging in user '{command.Email}'");
        }
    }
}