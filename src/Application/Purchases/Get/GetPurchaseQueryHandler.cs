using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Purchases.Get;

internal sealed class GetPurchaseQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetPurchaseQueryHandler> logger
) : IQueryHandler<GetPurchaseQuery, PagedResponse<PurchaseResponse>>
{
    public async Task<Result<PagedResponse<PurchaseResponse>>> HandleAsync(GetPurchaseQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTime? occurenceFrom = query.OccurenceFrom?.ToDateTime(TimeOnly.MinValue);
            DateTime? occurenceTo = query.OccurenceTo?.ToDateTime(TimeOnly.MaxValue);

            var result = dbContext.Purchases
                .Include(p => p.Products)
                .ThenInclude(p => p.Product)
                .Include(p => p.Tags)
                .Where(p =>
                    (string.IsNullOrEmpty(query.Search)
                        || p.Title.ToLower().Contains(query.Search.ToLower()))
                    && (!occurenceFrom.HasValue || p.OccurenceTime >= occurenceFrom)
                    && (!occurenceTo.HasValue || p.OccurenceTime <= occurenceTo)
                    && (!query.TotalFrom.HasValue
                        || p.Products.Sum(p => p.UnitCost * p.Quantity) >= query.TotalFrom)
                    && (!query.TotalTo.HasValue
                        || p.Products.Sum(p => p.UnitCost * p.Quantity) <= query.TotalTo)
                )
                .AsQueryable();
            
            result = query.SortOrder == SortOrder.ASC
                ? result.OrderBy(p => p.OccurenceTime)
                : result.OrderByDescending(p => p.OccurenceTime);

            var responses = result
                .Select(r => r.ToPurchaseResponse());

            return await PagedResponse<PurchaseResponse>.CreateAsync(responses, query.Page, query.PageSize);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetPurchaseQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting purchases with query '{query}' from DB",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetPurchaseQueryHandler),
                $"Unexpected error has occurred while getting purchases with query '{query}' from DB");
        }
    }
}
