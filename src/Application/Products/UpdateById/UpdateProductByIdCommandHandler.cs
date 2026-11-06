using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.UpdateById;

internal sealed class UpdateProductByIdCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<UpdateProductByIdCommandHandler> logger
) : ICommandHandler<UpdateProductByIdCommand, UpdateProductByIdResponse>
{
    public async Task<Result<UpdateProductByIdResponse>> HandleAsync(UpdateProductByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await dbContext.Products
                .Include(p => p.Detail)
                .ThenInclude(d => d.Brand)
                .Include(p => p.Detail)
                .ThenInclude(d => d.MeasureUnit)
                .Include(p => p.Tags)
                .Include(p => p.Prices.OrderByDescending(pr => pr.CreatedTime))
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (product == null)
            {
                return ProductErrors.NotFound(command.Id);
            }

            var existingProduct = await dbContext.Products
                .Select(p => new { p.Id, p.Name, p.Detail.BrandId })
                .FirstOrDefaultAsync(p => p.Name == command.Name && p.BrandId == product.Detail.BrandId,
                    cancellationToken);

            if (existingProduct != null
                && existingProduct.Id != product.Id)
            {
                return ProductErrors.AlreadyExists(command.Name, product.Detail.Brand!.Name,
                    product.Detail.Brand!.Id);
            }

            var unit = await dbContext.ProductUnits
                .FirstOrDefaultAsync(u => u.Id == command.UnitId, cancellationToken);

            if (unit == null)
            {
                return ProductErrors.UnitNotFound(command.UnitId);
            }

            if (product.Name != command.Name)
            {
                product.UpdateName(command.Name);
            }

            var detail = product.Detail;

            detail.UpdateDescription(command.Description);
            detail.UpdateMeasure(command.Measure, unit);

            var productTags = await TagsHelper.CreateSynchronizedTagsAsync(command.Tags, ProductTag.CreateNew,
                product.Tags, dbContext.ProductTags, cancellationToken);

            product.UpdateTags(productTags.ToList());

            var dtNow = dtProvider.UtcNow;

            var currentPrice = product.Prices.FirstOrDefault();
            if (currentPrice!.Value != command.Price)
            {
                var newPrice = ProductPrice.CreateNew(product.Id, command.Price, dtNow,
                    dtNow, null);
                dbContext.ProductPrices.Add(newPrice);

                currentPrice.UpdateValidToTime(dtNow);
                currentPrice = newPrice;
            }

            product.UpdateLastUpdatedTime(dtNow);

            await dbContext.SaveChangesAsync(cancellationToken);

            return product.ToUpdateByIdResponse(currentPrice);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating product with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateProductByIdCommandHandler),
                $"DB error has occurred while updating product with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateProductByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating product with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateProductByIdCommandHandler),
                $"Unexpected error has occurred while updating product with id '{command.Id}' to DB");
        }
    }
}