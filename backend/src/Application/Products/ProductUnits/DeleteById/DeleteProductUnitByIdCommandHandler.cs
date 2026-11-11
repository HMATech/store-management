using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared;

namespace Application.Products.ProductUnits.DeleteById;

internal sealed class DeleteProductUnitByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteProductUnitByIdCommandHandler> logger
) : ICommandHandler<DeleteProductUnitByIdCommand>
{
    public async Task<Result> HandleAsync(DeleteProductUnitByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var productUnit = await dbContext.ProductUnits
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (productUnit == null)
            {
                return ProductErrors.UnitNotFound(command.Id);
            }

            dbContext.ProductUnits.Remove(productUnit);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException pex
                && pex.SqlState == ApplicationErrorDefaults.PostgreSQLOnDeleteRestrictViolationCode)
            {
                logger.LogWarning("Attempting to delete product unit with id '{command.Id}' " +
                    "that is still referenced by other entities",
                    command.Id);
                return ProductErrors.UnitIsStillInUse(command.Id);
            }
            logger.LogError(ex, "DB error has occurred while deleting product unit with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeleteProductUnitByIdCommandHandler),
                $"DB error has occurred while deleting product unit with id '{command.Id}' from DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeleteProductUnitByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting product unit " +
                "with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeleteProductUnitByIdCommandHandler),
                "Unexpected error has occurred while deleting product unit " +
                $"with id '{command.Id}' from DB");
        }
    }
}