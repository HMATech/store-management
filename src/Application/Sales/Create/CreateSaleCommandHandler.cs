using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Sales.Create;

internal sealed class CreateSaleCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreateSaleCommandHandler> logger
) : ICommandHandler<CreateSaleCommand, CreateSaleResponse>
{
    public async Task<Result<CreateSaleResponse>> HandleAsync(CreateSaleCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var dtNow = dtProvider.UtcNow;

            var inputProductIds = command.ProductEntries.Select(e => e.Id);

            var products = await dbContext.Products
                .Select(p => new
                {
                    p.Id,
                    CurrentPrice = p.Prices.Select(pp => new { pp.Id, pp.ValidToTime })
                        .FirstOrDefault(pp => pp.ValidToTime == null)
                })
                .Where(p => inputProductIds.Contains(p.Id))
                .ToArrayAsync(cancellationToken);

            var invalidProducts = inputProductIds.ExceptBy(products.Select(p => p.Id), p => p);
            if (invalidProducts.Any())
            {
                return SaleErrors.ContainNonExistingProducts(invalidProducts);
            }

            var sale = Sale.CreateNew(command.Title, command.OccurrenceTime, dtNow);

            if (command.Tags.Length > 0)
            {
                IEnumerable<SaleTag> purchaseTags = [];

                purchaseTags = await TagsHelper.CreateSynchronizedTagsAsync(command.Tags, SaleTag.CreateNew, sale.Tags,
                    dbContext.SaleTags, cancellationToken);
                
                sale.UpdateTags(purchaseTags.ToList());
            }

            var productEntries = products.Select(p => SaleProductEntry.CreateNew(
                sale.Id, p.Id, p.CurrentPrice!.Id,
                command.ProductEntries.Where(e => e.Id == p.Id).Sum(e => e.Quantity)
            ));

            sale.UpdateProductEntries(productEntries.ToList());
            dbContext.Sales.Add(sale);

            await dbContext.SaveChangesAsync(cancellationToken);

            return sale.ToCreateResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new sale '{command.Title}' to DB",
                command.Title);
            return ApplicationErrors.DBOperationError(nameof(CreateSaleCommandHandler),
                $"DB error has occurred while adding new sale '{command.Title}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreateSaleCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new sale '{command.Title}' to DB",
                command.Title);
            return ApplicationErrors.UnexpectedError(nameof(CreateSaleCommandHandler),
                $"Unexpected error has occurred while adding new sale '{command.Title}' to DB");
        }
    }
}