using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.UpdateById;

internal sealed class UpdateRoleByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UpdateRoleByIdCommandHandler> logger
) : ICommandHandler<UpdateRoleByIdCommand, UpdateRoleByIdResponse>
{
    public async Task<Result<UpdateRoleByIdResponse>> HandleAsync(UpdateRoleByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await dbContext.Roles
                .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);

            if (role == null)
            {
                return RoleErrors.NotFound(command.Id);
            }

            var existingRoleWithName = await dbContext.Roles
                .FirstOrDefaultAsync(r => r.Name.Normalized == command.Name.ToLower().Trim(),
                    cancellationToken);

            if (existingRoleWithName != null
                && existingRoleWithName.Id != role.Id)
            {
                return RoleErrors.AlreadyExist(command.Name);
            }

            role.UpdateName(command.Name);
            role.UpdateDescription(command.Description);

            dbContext.Roles.Update(role);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new UpdateRoleByIdResponse(role.Name.Value, role.Name.Normalized, role.Description);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating role with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateRoleByIdCommandHandler),
                $"DB error has occurred while updating role with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateRoleByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating role with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateRoleByIdCommandHandler),
                $"Unexpected error has occurred updating role with id '{command.Id}' to DB");
        }
    }
}