using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Users.GetById;
using Microsoft.Extensions.Logging;
using Shared;
using Application.Common;

namespace Application.Users.Get;

internal sealed class GetUserQueryHandler(IApplicationDbContext dbContext,
    ILogger<GetUserByIdQueryHandler> logger)
    : IQueryHandler<GetUserQuery, PagedResponse<UserResponse>>
{
    public async Task<Result<PagedResponse<UserResponse>>> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var users = dbContext.Users.AsQueryable();
            if (!string.IsNullOrEmpty(query.Search))
            {
                users = users
                    .Where(u =>
                        u.Name.First.Contains(query.Search)
                            || (u.Name.Last != null && u.Name.Last.Contains(query.Search)));
            }

            var userResponses = users
                .OrderBy(u => u.CreatedTime)
                .Select(u => new UserResponse(u.Id, u.Email, u.Name.First, u.Name.Last));
            return await PagedResponse<UserResponse>.CreateAsync(userResponses, query.Page, query.PageSize);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetUserQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying users with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetUserQueryHandler),
                $"Unexpected error has occurred while querying users with '{query}'");
        }
    }
}