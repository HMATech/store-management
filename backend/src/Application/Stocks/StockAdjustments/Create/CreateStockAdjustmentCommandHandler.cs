using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Domain.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Stocks.StockAdjustments.Create;

internal sealed class CreateStockAdjustmentCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreateStockAdjustmentCommandHandler> logger
) : ICommandHandler<CreateStockAdjustmentCommand, CreateStockAdjustmentResponse>
{
    public async Task<Result<CreateStockAdjustmentResponse>> HandleAsync(CreateStockAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await dbContext.Products.AnyAsync(p => p.Id == command.ProductId, cancellationToken))
            {
                return ProductErrors.NotFound(command.ProductId);
            }

            var adjustment = StockAdjustment.CreateNew(command.ProductId, command.Quantity, command.Reason,
                command.OccurrenceTime, dtProvider.UtcNow);

            dbContext.StockAdjustments.Add(adjustment);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new CreateStockAdjustmentResponse(adjustment.Id);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding stock adjustment for product " +
                "with id '{command.ProductId}' to DB",
                command.ProductId);
            return ApplicationErrors.DBOperationError(nameof(CreateStockAdjustmentCommandHandler),
                "DB error has occurred while adding stock adjustment for product " +
                $"with id '{command.ProductId}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreateStockAdjustmentCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding stock adjustment for product " +
                "with id '{command.ProductId}' to DB",
                command.ProductId);
            return ApplicationErrors.UnexpectedError(nameof(CreateStockAdjustmentCommandHandler),
                "Unexpected error has occurred while adding stock adjustment for product " +
                $"with id '{command.ProductId}' to DB");
        }
    }
}
