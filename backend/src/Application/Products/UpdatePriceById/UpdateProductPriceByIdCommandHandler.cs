using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.UpdatePriceById;

internal sealed class UpdateProductPriceByIdCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<UpdateProductPriceByIdCommandHandler> logger
) : ICommandHandler<UpdateProductPriceByIdCommand, UpdateProductPriceByIdResponse>
{
    public async Task<Result<UpdateProductPriceByIdResponse>> HandleAsync(UpdateProductPriceByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await dbContext.Products
                .Select(p => new
                {
                    p.Id,
                    CurrentPrice = p.Prices.FirstOrDefault(pp => pp.ValidToTime == null)
                })
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (product == null)
            {
                return ProductErrors.NotFound(command.Id);
            }

            var dtNow = dtProvider.UtcNow;

            product.CurrentPrice!.UpdateValidToTime(dtNow);

            dbContext.ProductPrices.Update(product.CurrentPrice);

            var newPrice = ProductPrice.CreateNew(product.Id,
                command.Value, dtNow, dtNow, null);

            dbContext.ProductPrices.Add(newPrice);

            await dbContext.SaveChangesAsync(cancellationToken);

            return new UpdateProductPriceByIdResponse(
                newPrice.Id, product.Id, product.CurrentPrice.Id,
                newPrice.Value, newPrice.ValidFromTime
            );
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating product with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateProductPriceByIdCommandHandler),
                $"DB error has occurred while updating product with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateProductPriceByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating product with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateProductPriceByIdCommandHandler),
                $"Unexpected error has occurred while updating product with id '{command.Id}' to DB");
        }
    }
}