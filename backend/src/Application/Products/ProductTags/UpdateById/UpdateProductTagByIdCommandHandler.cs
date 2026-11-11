using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductTags.UpdateById;

internal sealed class UpdateProductTagByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UpdateProductTagByIdCommandHandler> logger
) : ICommandHandler<UpdateProductTagByIdCommand, UpdateProductTagByIdResponse>
{
    public async Task<Result<UpdateProductTagByIdResponse>> HandleAsync(UpdateProductTagByIdCommand command,
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

            var inputTag = ProductTag.CreateNew(command.Name);

            var sameProductTag = await dbContext.ProductTags
                .Select(p => new { p.Id, p.Name.Normalized })
                .FirstOrDefaultAsync(p => p.Normalized == inputTag.Name.Normalized,
                    cancellationToken);

            if (sameProductTag != null)
            {
                if (sameProductTag.Id != productTag.Id)
                {
                    return ProductErrors.TagAlreadyExists(command.Name);
                }

                if (productTag.Name.Value == command.Name)
                {
                    return productTag.ToUpdateByIdResponse();
                }
            }

            productTag.UpdateName(command.Name);

            await dbContext.SaveChangesAsync(cancellationToken);

            return productTag.ToUpdateByIdResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating product tag with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateProductTagByIdCommandHandler),
                $"DB error has occurred while updating product tag with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateProductTagByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating product tag " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateProductTagByIdCommandHandler),
                "Unexpected error has occurred while updating product tag " +
                $"with id '{command.Id}' to DB");
        }
    }
}