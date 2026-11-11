using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Brands;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.Create;

internal sealed class CreateProductCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreateProductCommandHandler> logger
) : ICommandHandler<CreateProductCommand, CreateProductResponse>
{
    public async Task<Result<CreateProductResponse>> HandleAsync(CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var dtNow = dtProvider.UtcNow;

        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(b => b.Id == command.BrandId, cancellationToken);

        if (brand == null)
        {
            return BrandErrors.NotFound(command.BrandId);
        }
        else
        {
            var existingProduct = await dbContext.Products
                .Select(p => new { p.Name, p.Detail.BrandId })
                .FirstOrDefaultAsync(p => p.Name == command.Name && p.BrandId == brand.Id,
                    cancellationToken);

            if (existingProduct != null)
            {
                return ProductErrors.AlreadyExists(command.Name, brand.Name, brand.Id);
            }
        }

        var unit = await dbContext.ProductUnits
            .FirstOrDefaultAsync(pu => pu.Id == command.UnitId,
            cancellationToken);

        if (unit == null)
        {
            return ProductErrors.UnitNotFound(command.UnitId);
        }

        try
        {
            var product = Product.CreateNew(command.Name, command.Description, brand.Id,
                command.Measure, unit.Id, dtNow,
                StockDefinition.CreateNew(command.StockDefinition?.MinimumQuantity));

            var tags = await TagsHelper.CreateSynchronizedTagsAsync(command.Tags, ProductTag.CreateNew,
                product.Tags, dbContext.ProductTags, cancellationToken);

            product.UpdateTags(tags.ToList());

            var price = ProductPrice.CreateNew(product.Id, command.Price, dtNow, dtNow, null);
            product.AddPrice(price);

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(cancellationToken);

            return product.ToCreateResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new product '{brand.Name} - {command.Name}' to DB",
                brand.Name, command.Name);
            return ApplicationErrors.DBOperationError(nameof(CreateProductCommandHandler),
                $"DB error has occurred while adding new product '{brand.Name} - {command.Name}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreateProductCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new product " +
                "'{brand.Name} - {command.Name}' to DB",
                brand.Name, command.Name);
            return ApplicationErrors.UnexpectedError(nameof(CreateProductCommandHandler),
                $"Unexpected error has occurred while adding new product '{brand.Name} - {command.Name}' to DB");
        }
    }
}