using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.DeleteById;

internal sealed class DeleteRoleByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteRoleByIdCommandHandler> logger
) : ICommandHandler<DeleteRoleByIdCommand>
{
    public async Task<Result> HandleAsync(DeleteRoleByIdCommand command,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

        if (role == null)
        {
            return RoleErrors.NotFound(command.Id);
        }

        try
        {
            dbContext.Roles.Remove(role);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while deleting role with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeleteRoleByIdCommandHandler),
                $"DB error has occurred while deleting role with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeleteRoleByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting role with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeleteRoleByIdCommandHandler),
                $"Unexpected error has occurred deleting role with id '{command.Id}' to DB");
        }
    }
}