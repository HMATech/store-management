using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.UnassignUser;

internal sealed class UnassignUserCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UnassignUserCommandHandler> logger
) : ICommandHandler<UnassignUserCommand>
{
    public async Task<Result> HandleAsync(UnassignUserCommand command,
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

        if (userRole == null)
        {
            return RoleErrors.NotAssigned(role.Id, user.Email);
        }

        try
        {
            dbContext.UserRoles.Remove(userRole);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while unassigning role with id " +
                "'{command.RoleId}' from user '{command.User}'",
                command.RoleId, command.User);
            return ApplicationErrors.DBOperationError(nameof(UnassignUserCommandHandler),
                "DB error has occurred while unassigning role with id " +
                $"'{command.RoleId}' from user '{command.User}'");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UnassignUserCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while unassigning role with id " +
                "'{command.RoleId}' from user '{command.User}'",
                command.RoleId, command.User);
            return ApplicationErrors.UnexpectedError(nameof(UnassignUserCommandHandler),
                "Unexpected error has occurred while unassigning role with id " +
                $"'{command.RoleId}' from user '{command.User}'");
        }
    }
}