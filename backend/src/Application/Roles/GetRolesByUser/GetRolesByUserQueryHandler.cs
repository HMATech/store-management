using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.GetRolesByUser;

internal sealed class GetRolesByUserQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetRolesByUserQueryHandler> logger)
        : IQueryHandler<GetRolesByUserQuery, List<UserRoleResponse>>
{
    public async Task<Result<List<UserRoleResponse>>> HandleAsync(GetRolesByUserQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = new { Id = Guid.Empty, Email = string.Empty };

            if (Guid.TryParse(query.User, out var uid))
            {
                user = await dbContext.Users
                    .Select(u => new { u.Id, u.Email } )
                    .FirstOrDefaultAsync(u => u.Id == uid, cancellationToken);

                if (user == null)
                {
                    return UserErrors.NotFound(uid);
                }
            }
            else
            {
                user = await dbContext.Users
                    .Select(u => new { u.Id, u.Email })
                    .FirstOrDefaultAsync(u => u.Email == query.User, cancellationToken);

                if (user == null)
                {
                    return UserErrors.NotFound(query.User);
                }
            }

            var userRoles = dbContext.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.Id)
                .OrderByDescending(ur => ur.User!.Email)
                .Select(ur => new UserRoleResponse(ur.RoleId, ur.Role!.Name.Value, 
                    ur.Role.Name.Normalized, ur.AssignedTime));

            return await userRoles.ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetRolesByUserQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting roles of user '{query.User}'",
                query.User);
            return ApplicationErrors.UnexpectedError(nameof(GetRolesByUserQueryHandler),
                $"Unexpected error has occurred while getting while getting roles of user '{query.User}'");
        }
    }
}
