using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Users;
using Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.GetUsersByName;

internal sealed class GetUsersByNameQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetUsersByNameQueryHandler> logger)
        : IQueryHandler<GetUsersByNameQuery, List<UserResponse>>
{
    public async Task<Result<List<UserResponse>>> HandleAsync(GetUsersByNameQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var normalizedName = query.Name.ToLower().Trim();

            var role = await dbContext.Roles
                .Select(r => new { r.Id, r.Name.Normalized })
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Normalized == normalizedName, cancellationToken);

            if (role == null)
            {
                return RoleErrors.NotFound(query.Name);
            }

            var users = dbContext.UserRoles
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == role.Id)
                .OrderByDescending(ur => ur.User!.Email)
                .Select(ur => new UserResponse(ur.User!.Id, ur.User.Email,
                        ur.User.Name.First, ur.User.Name.Last));

            return await users.ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetUsersByNameQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting users of role with name '{query.Name}'",
                query.Name);
            return ApplicationErrors.UnexpectedError(nameof(GetUsersByNameQueryHandler),
                $"Unexpected error has occurred while getting users of role with name '{query.Name}'");
        }
    }
}
