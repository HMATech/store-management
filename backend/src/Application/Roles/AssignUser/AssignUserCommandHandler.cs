using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.AssignUser;

internal sealed class AssignUserCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<AssignUserCommandHandler> logger
) : ICommandHandler<AssignUserCommand, AssignUserResponse>
{
    public async Task<Result<AssignUserResponse>> HandleAsync(AssignUserCommand command,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (role == null)
        {
            return RoleErrors.NotFound(command.RoleId);
        }

        User? user;

        if (Guid.TryParse(command.User, out var uid))
        {
            user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);

            if (user == null)
            {
                return UserErrors.NotFound(uid);
            }
        }
        else
        {
            user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == command.User, cancellationToken);
            
            if (user == null)
            {
                return UserErrors.NotFound(command.User);
            }
        }

        var userRole = await dbContext.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id,
                cancellationToken);

        if (userRole != null)
        {
            return RoleErrors.AlreadyAssigned(role.Id, user.Email);
        }

        try
        {
            userRole = UserRole.CreateNew(user.Id, role.Id, dtProvider.UtcNow);

            dbContext.UserRoles.Add(userRole);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new AssignUserResponse(role.Id, role.Name.Normalized, user.Id);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while assigning role with id " +
                "'{command.RoleId}' to user '{command.User}'",
                command.RoleId, command.User);
            return ApplicationErrors.DBOperationError(nameof(AssignUserCommandHandler),
                "DB error has occurred while assigning role with id " +
                $"'{command.RoleId}' to user '{command.User}'");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(AssignUserCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while assigning role with id " +
                "'{command.RoleId}' to user '{command.User}'",
                command.RoleId, command.User);
            return ApplicationErrors.UnexpectedError(nameof(AssignUserCommandHandler),
                "Unexpected error has occurred while assigning role with id " +
                $"'{command.RoleId}' to user '{command.User}'");
        }
    }
}