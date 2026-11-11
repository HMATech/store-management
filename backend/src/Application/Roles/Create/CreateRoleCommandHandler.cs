using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.Create;

internal sealed class CreateRoleCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreateRoleCommandHandler> logger
) : ICommandHandler<CreateRoleCommand, CreateRoleResponse>
{
    public async Task<Result<CreateRoleResponse>> HandleAsync(CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .FirstOrDefaultAsync(
                r => r.Name.Normalized == command.Role.ToLower().Trim(),
                cancellationToken
            );

        if (role != null)
        {
            return RoleErrors.AlreadyExist(command.Role);
        }

        try
        {
            role = Role.CreateNew(command.Role, command.Description, dtProvider.UtcNow);
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new CreateRoleResponse(role.Id);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new role '{command.Role}' to DB",
                command.Role);
            return ApplicationErrors.DBOperationError(nameof(CreateRoleCommandHandler),
                $"DB error has occurred while adding new role '{command.Role}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreateRoleCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new role '{command.Role}' to DB",
                command.Role);
            return ApplicationErrors.UnexpectedError(nameof(CreateRoleCommandHandler),
                $"Unexpected error has occurred while adding new role '{command.Role}' to DB");
        }
    }
}