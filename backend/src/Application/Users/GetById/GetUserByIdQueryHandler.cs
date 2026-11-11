using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Users.GetById;

internal sealed class GetUserByIdQueryHandler(IApplicationDbContext dbContext,
    ILogger<GetUserByIdQueryHandler> logger)
        : IQueryHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<Result<UserResponse>> HandleAsync(GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == query.UserId,
                 cancellationToken);
            if (user == null)
            {
                return UserErrors.NotFound(query.UserId);
            }

            return new UserResponse(user.Id, user.Email, user.Name.First, user.Name.Last);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetUserByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting user with id '{query.UserId}'",
                query.UserId);
            return ApplicationErrors.UnexpectedError(nameof(GetUserByIdQueryHandler),
                $"Unexpected error has occurred while getting user with id '{query.UserId}'");
        }
    }
}
