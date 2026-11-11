using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Sales.Get;

internal sealed class GetSaleQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetSaleQueryHandler> logger
) : IQueryHandler<GetSaleQuery, PagedResponse<SaleResponse>>
{
    public async Task<Result<PagedResponse<SaleResponse>>> HandleAsync(GetSaleQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTime? occurenceFrom = query.OccurenceFrom?.ToDateTime(TimeOnly.MinValue);
            DateTime? occurenceTo = query.OccurenceTo?.ToDateTime(TimeOnly.MaxValue);

            var result = dbContext.Sales
                .Include(p => p.Products)
                .ThenInclude(p => p.Product)
                .Include(p => p.Products)
                .ThenInclude(p => p.ProductPrice)
                .Include(p => p.Tags)
                .Where(p =>
                    (string.IsNullOrEmpty(query.Search)
                        || p.Title.ToLower().Contains(query.Search.ToLower()))
                    && (!occurenceFrom.HasValue || p.OccurenceTime >= occurenceFrom)
                    && (!occurenceTo.HasValue || p.OccurenceTime <= occurenceTo)
                    && (!query.TotalFrom.HasValue
                        || p.Products.Sum(p => p.ProductPrice!.Value * p.Quantity) >= query.TotalFrom)
                    && (!query.TotalTo.HasValue
                        || p.Products.Sum(p => p.ProductPrice!.Value * p.Quantity) <= query.TotalTo)
                )
                .AsQueryable();
            
            result = query.SortOrder == SortOrder.ASC
                ? result.OrderBy(p => p.OccurenceTime)
                : result.OrderByDescending(p => p.OccurenceTime);

            var responses = result
                .Select(r => r.ToSaleResponse());

            return await PagedResponse<SaleResponse>.CreateAsync(responses, query.Page, query.PageSize);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetSaleQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting sales with query '{query}' from DB",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetSaleQueryHandler),
                $"Unexpected error has occurred while getting sales with query '{query}' from DB");
        }
    }
}
