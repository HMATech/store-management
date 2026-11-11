using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Stocks.StockAdjustments.GetById;

internal sealed class GetStockAdjustmentByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetStockAdjustmentByIdQueryHandler> logger
) : IQueryHandler<GetStockAdjustmentByIdQuery, StockAdjustmentResponse>
{
    public async Task<Result<StockAdjustmentResponse>> HandleAsync(GetStockAdjustmentByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var adjustment = await dbContext.StockAdjustments
                .FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken);

            if (adjustment == null)
            {
                return StockErrors.AdjustmentNotFound(query.Id);
            }

            return adjustment.ToStockAdjustmentResponse();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetStockAdjustmentByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting stock adjustment " +
                "with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetStockAdjustmentByIdQueryHandler),
                $"Unexpected error has occurred while getting stock adjustment with id '{query.Id}' from DB");
        }
    }
}
