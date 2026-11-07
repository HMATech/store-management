using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Stocks.StockAdjustments.Get;

internal sealed class GetStockAdjustmentQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetStockAdjustmentQueryHandler> logger
) : IQueryHandler<GetStockAdjustmentQuery, PagedResponse<StockAdjustmentResponse>>
{
    public async Task<Result<PagedResponse<StockAdjustmentResponse>>> HandleAsync(GetStockAdjustmentQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var adjustments = dbContext.StockAdjustments
                .Where(a => query.ProductId == null || a.ProductId == query.ProductId)
                .OrderByDescending(a => a.OccurrenceTime)
                .ThenByDescending(a => a.CreatedTime)
                .Select(a => a.ToStockAdjustmentResponse());

            return await PagedResponse<StockAdjustmentResponse>.CreateAsync(adjustments, query.Page, query.PageSize);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetStockAdjustmentQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying stock adjustments with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetStockAdjustmentQueryHandler),
                $"Unexpected error has occurred while querying stock adjustments with '{query}'");
        }
    }
}
