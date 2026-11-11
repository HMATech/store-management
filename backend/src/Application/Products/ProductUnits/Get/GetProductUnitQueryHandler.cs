using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductUnits.Get;

internal sealed class GetProductUnitQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductUnitQueryHandler> logger
) : IQueryHandler<GetProductUnitQuery, List<ProductUnitResponse>>
{
    public async Task<Result<List<ProductUnitResponse>>> HandleAsync(GetProductUnitQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = dbContext.ProductUnits
                .Where(p => string.IsNullOrEmpty(query.Search) || p.Name.Value.Contains(query.Search))
                .AsQueryable();

            result = query.SortOrder == SortOrder.ASC
                ? result.OrderBy(p => p.Name.Value)
                : result.OrderByDescending(p => p.Name.Value);

            var responses = result
                .Select(p => p.ToProductUnitResponse());

            return await responses.ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductUnitQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying product units with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetProductUnitQueryHandler),
                $"Unexpected error has occurred while querying product units with '{query}'");
        }
    }
}