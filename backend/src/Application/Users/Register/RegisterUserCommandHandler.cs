using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(IPasswordHasher passwordHasher,
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<RegisterUserCommandHandler> logger) 
        : ICommandHandler<RegisterUserCommand, RegisterResponse>
{
    public async Task<Result<RegisterResponse>> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(u => u.Email == command.Email, cancellationToken))
        {
            return UserErrors.EmailAlreadyRegistered(command.Email);
        }

        try
        {
            var password = passwordHasher.Hash(command.Password);

            var user = User.CreateNew(command.Email, password, command.FirstName, command.LastName, dtProvider.UtcNow);

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new RegisterResponse(user.Id, user.Email, user.Name.First, user.Name.Last);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new user '{command.Email}' to DB",
                command.Email);
            return ApplicationErrors.DBOperationError(nameof(RegisterUserCommandHandler),
                $"DB error has occurred while adding new user '{command.Email}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(RegisterUserCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new user '{command.Email}' to DB",
                command.Email);
            return ApplicationErrors.UnexpectedError(nameof(RegisterUserCommandHandler),
                $"Unexpected error has occurred while adding new user '{command.Email}' to DB");
        }
    }
}