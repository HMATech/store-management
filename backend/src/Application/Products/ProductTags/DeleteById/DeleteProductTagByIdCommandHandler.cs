using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared;

namespace Application.Products.ProductTags.DeleteById;

internal sealed class DeleteProductTagByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteProductTagByIdCommandHandler> logger
) : ICommandHandler<DeleteProductTagByIdCommand>
{
    public async Task<Result> HandleAsync(DeleteProductTagByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var productTag = await dbContext.ProductTags
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (productTag == null)
            {
                return ProductErrors.TagNotFound(command.Id);
            }

            dbContext.ProductTags.Remove(productTag);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Succeed();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while deleting product tag with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(DeleteProductTagByIdCommandHandler),
                $"DB error has occurred while deleting product tag with id '{command.Id}' from DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(DeleteProductTagByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while deleting product tag " +
                "with id '{command.Id}' from DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(DeleteProductTagByIdCommandHandler),
                "Unexpected error has occurred while deleting product tag " +
                $"with id '{command.Id}' from DB");
        }
    }
}