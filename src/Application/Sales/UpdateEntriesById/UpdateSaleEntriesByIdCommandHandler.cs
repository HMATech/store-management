using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Sales.UpdateEntriesById;

// TODO: REFACTOR - FOR BETTER UPDATE FLOW
internal sealed class UpdateSaleEntriesByIdCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<UpdateSaleEntriesByIdCommandHandler> logger
) : ICommandHandler<UpdateSaleEntriesByIdCommand, UpdateSaleEntriesByIdResponse>
{
    public async Task<Result<UpdateSaleEntriesByIdResponse>> HandleAsync(UpdateSaleEntriesByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var sale = await dbContext.Sales
                .Include(p => p.Products)
                .ThenInclude(p => p.Product)
                .Include(p => p.Products)
                .ThenInclude(p => p.ProductPrice)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (sale == null)
            {
                return SaleErrors.NotFound(command.Id);
            }

            var dtNow = dtProvider.UtcNow;

            var inputEntryIds = command.ProductEntries.Select(e => e.Id);

            var currentEntryIds = sale.Products.Select(pe => pe.Id);
            var foreignEntryIds = inputEntryIds
                .Where(i => i.HasValue && i.Value != Guid.Empty && !currentEntryIds.Contains(i.Value))
                .Cast<Guid>();

            if (foreignEntryIds.Any())
            {
                return SaleErrors.ContainForeignEntries(sale.Id, foreignEntryIds);
            }

            var existingEntryIds = currentEntryIds.IntersectBy(inputEntryIds, i => i);

            var inputNewEntries = command.ProductEntries.Where(pe => !pe.Id.HasValue || pe.Id.Value == Guid.Empty);
            var inputExistingEntries = command.ProductEntries.Where(pe => pe.Id.HasValue && pe.Id.Value != Guid.Empty);

            var existingEntries = sale.Products.Where(pe => existingEntryIds.Contains(pe.Id));

            var existingInputProductIds = existingEntries.Select(e => e.ProductId);
            var inputNewProductIds = inputNewEntries.Select(e => e.ProductId);

            var inputCombinedProductIds = existingInputProductIds.Concat(inputNewProductIds);
            var removedProducts = sale.Products.Except(existingEntries);

            var products = await dbContext.Products
            .Select(p => new
            {
                p.Id,
                CurrentPrice = p.Prices.Select(pp => new { pp.Id, pp.ValidToTime })
                    .FirstOrDefault(pp => pp.ValidToTime == null)
            })
            .Where(p => inputCombinedProductIds.Contains(p.Id))
            .ToArrayAsync(cancellationToken);

            var invalidProducts = inputCombinedProductIds.ExceptBy(products.Select(p => p.Id), p => p);
            if (invalidProducts.Any())
            {
                return SaleErrors.ContainNonExistingProducts(invalidProducts);
            }

            if (existingEntries.Any())
            {
                var inputQuantities = inputExistingEntries
                    .GroupBy(e => e.Id!.Value)
                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Quantity));

                foreach (var entry in existingEntries)
                {
                    if (inputQuantities.TryGetValue(entry.Id, out var quantity))
                    {
                        entry.UpdateQuantity(quantity);
                    }
                }
            }

            if (removedProducts.Any())
            {
                dbContext.SaleProductEntries.RemoveRange(removedProducts);
            }

            if (inputNewEntries.Any())
            {
                var inputNewProducts = products.Where(p => inputNewProductIds.Contains(p.Id));

                var newProductEntries = inputNewProducts.Select(p => SaleProductEntry.CreateNew(
                    sale.Id, p.Id, p.CurrentPrice!.Id,
                    inputNewEntries.Where(e => e.ProductId == p.Id).Sum(e => e.Quantity)
                ));

                dbContext.SaleProductEntries.AddRange(newProductEntries);
            }

            sale.UpdateLastUpdatedTime(dtNow);

            await dbContext.SaveChangesAsync(cancellationToken);

            sale.Products.Clear();
            (await dbContext.SaleProductEntries
                .Include(pe => pe.Product)
                .Include(pe => pe.ProductPrice)
                .AsNoTracking()
                .Where(pe => pe.SaleId == sale.Id)
                .ToListAsync(cancellationToken)
            ).ForEach(sale.Products.Add);

            return sale.ToUpdateEntriesByIdResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating sale's product entries " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateSaleEntriesByIdCommandHandler),
                $"DB error has occurred while updating sale's product entries with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateSaleEntriesByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating sale's product entries " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateSaleEntriesByIdCommandHandler),
                "Unexpected error has occurred while updating sale's product entries " +
                    $"with id '{command.Id}' to DB");
        }
    }
}