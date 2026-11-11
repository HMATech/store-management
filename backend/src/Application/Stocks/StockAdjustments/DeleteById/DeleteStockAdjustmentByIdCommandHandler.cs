using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Stocks.StockAdjustments.DeleteById;

internal sealed class DeleteStockAdjustmentByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteStockAdjustmentByIdCommandHandler> logger
) : ICommandHandler<DeleteStockAdjustmentByIdCommand>
{
    public async Task<Result> HandleAsync(DeleteStockAdjustmentByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var adjustment = await dbContext.StockAdjustments
                .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

            if (adjustment == null)
            {
                return StockErrors.AdjustmentNotFound(command.Id);
            }

            dbContext.StockAdjustments.Remove(adjustment);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while deleting stock adjustment " +
                "with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeleteStockAdjustmentByIdCommandHandler),
                $"DB error has occurred while deleting stock adjustment with id '{command.Id}' from DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeleteStockAdjustmentByIdCommandHandler),
                ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting stock adjustment " +
                "with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeleteStockAdjustmentByIdCommandHandler),
                $"Unexpected error has occurred while deleting stock adjustment with id '{command.Id}' from DB");
        }
    }
}
