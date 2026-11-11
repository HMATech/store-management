using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductPrices.UpdateById;

internal sealed class UpdateProductPriceByIdCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UpdateProductPriceByIdCommandHandler> logger
) : ICommandHandler<UpdateProductPriceByIdCommand, UpdateProductPriceByIdResponse>
{
    public async Task<Result<UpdateProductPriceByIdResponse>> HandleAsync(UpdateProductPriceByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var productPrice = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (productPrice == null)
            {
                return ProductErrors.PriceNotFound(command.Id);
            }

            productPrice.UpdateValue(command.Value);

            await dbContext.SaveChangesAsync(cancellationToken);

            return productPrice.ToUpdateByIdResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating product price with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateProductPriceByIdCommandHandler),
                $"DB error has occurred while updating product price with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateProductPriceByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating product price " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateProductPriceByIdCommandHandler),
                "Unexpected error has occurred while updating product price " +
                $"with id '{command.Id}' to DB");
        }
    }
}