using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.GetById;

internal sealed class GetRoleByIdQueryHandler(IApplicationDbContext dbContext,
    ILogger<GetRoleByIdQueryHandler> logger)
        : IQueryHandler<GetRoleByIdQuery, RoleResponse>
{
    public async Task<Result<RoleResponse>> HandleAsync(GetRoleByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await dbContext.Roles.FirstOrDefaultAsync(u => u.Id == query.Id,
                 cancellationToken);

            if (role == null)
            {
                return RoleErrors.NotFound(query.Id);
            }

            return new RoleResponse(role.Id, role.Name.Value,
                role.Name.Normalized, role.Description, role.CreatedTime);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetRoleByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting role with id '{query.Id}'",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetRoleByIdQueryHandler),
                $"Unexpected error has occurred while getting role with id '{query.Id}'");
        }
    }
}
