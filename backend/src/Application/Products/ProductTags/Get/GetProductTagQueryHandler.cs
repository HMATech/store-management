using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductTags.Get;

internal sealed class GetProductTagQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductTagQueryHandler> logger
) : IQueryHandler<GetProductTagQuery, List<ProductTagResponse>>
{
    public async Task<Result<List<ProductTagResponse>>> HandleAsync(GetProductTagQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = dbContext.ProductTags
                .Where(p => string.IsNullOrEmpty(query.Search) || p.Name.Value.Contains(query.Search))
                .AsQueryable();

            result = query.SortOrder == SortOrder.ASC
                ? result.OrderBy(p => p.Name.Value)
                : result.OrderByDescending(p => p.Name.Value);

            var responses = result
                .Select(p => p.ToProductTagResponse());

            return await responses.ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductTagQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying product tags with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetProductTagQueryHandler),
                $"Unexpected error has occurred while querying product tags with '{query}'");
        }
    }
}