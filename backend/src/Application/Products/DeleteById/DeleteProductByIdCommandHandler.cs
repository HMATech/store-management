using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared;

namespace Application.Products.DeleteById;

internal sealed class DeleteProductByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteProductByIdCommandHandler> logger
) : ICommandHandler<DeleteProductByIdCommand>
{
    public async Task<Result> HandleAsync(DeleteProductByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await dbContext.Products
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (product == null)
            {
                return ProductErrors.NotFound(command.Id);
            }

            dbContext.Products.Remove(product);

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
                return ProductErrors.InUse(command.Id);
            }
            logger.LogError(ex, "DB error has occurred while deleting product with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeleteProductByIdCommandHandler),
                $"DB error has occurred while deleting product with id '{command.Id}' from DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeleteProductByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting product with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeleteProductByIdCommandHandler),
                $"Unexpected error has occurred while deleting product with id '{command.Id}' from DB");
        }
    }
}