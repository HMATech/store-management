using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Sales.DeleteById;

internal sealed class DeleteSaleByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteSaleByIdCommandHandler> logger
) : ICommandHandler<DeleteSaleByIdCommand>
{
    public async Task<Result> HandleAsync(DeleteSaleByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var sale = await dbContext.Sales
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (sale == null)
            {
                return SaleErrors.NotFound(command.Id);
            }

            dbContext.Sales.Remove(sale);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while deleting sale with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeleteSaleByIdCommandHandler),
                $"DB error has occurred while deleting sale with id '{command.Id}' from DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeleteSaleByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting sale with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeleteSaleByIdCommandHandler),
                $"Unexpected error has occurred while deleting sale with id '{command.Id}' from DB");
        }
    }
}