using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Purchases.DeleteById;

internal sealed class DeletePurchaseByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeletePurchaseByIdCommandHandler> logger
) : ICommandHandler<DeletePurchaseByIdCommand>
{
    public async Task<Result> HandleAsync(DeletePurchaseByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var purchase = await dbContext.Purchases
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (purchase == null)
            {
                return PurchaseErrors.NotFound(command.Id);
            }

            dbContext.Purchases.Remove(purchase);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while deleting purchase with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeletePurchaseByIdCommandHandler),
                $"DB error has occurred while deleting purchase with id '{command.Id}' from DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeletePurchaseByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting purchase with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeletePurchaseByIdCommandHandler),
                $"Unexpected error has occurred while deleting purchase with id '{command.Id}' from DB");
        }
    }
}