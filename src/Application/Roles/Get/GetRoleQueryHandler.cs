using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Roles.Get;

internal sealed class GetRoleQueryHandler(IApplicationDbContext dbContext,
    ILogger<GetRoleQuery> logger)
        : IQueryHandler<GetRoleQuery, List<RoleResponse>>
{
    public async Task<Result<List<RoleResponse>>> HandleAsync(GetRoleQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var search = query.Search?.ToLower().Trim();

            var roles = dbContext.Roles
                .Where(r => string.IsNullOrEmpty(search) || r.Name.Normalized.Contains(search))
                .AsQueryable();

            roles = query.SortOrder == SortOrder.ASC
                ? roles.OrderBy(r => r.Name.Normalized)
                : roles.OrderByDescending(r => r.Name.Normalized);

            var response = await roles
                .Select(role => new RoleResponse(role.Id, role.Name.Value,
                    role.Name.Normalized, role.Description, role.CreatedTime))
                .ToListAsync(cancellationToken);

            return response;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetRoleQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying roles with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetRoleQueryHandler),
                $"Unexpected error has occurred while querying roles with '{query}'");
        }
    }
}
