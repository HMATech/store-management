using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Purchases.UpdateEntriesById;

// TODO: REFACTOR - FOR BETTER UPDATE FLOW
internal sealed class UpdatePurchaseEntriesByIdCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<UpdatePurchaseEntriesByIdCommandHandler> logger
) : ICommandHandler<UpdatePurchaseEntriesByIdCommand, UpdatePurchaseEntriesByIdResponse>
{
    public async Task<Result<UpdatePurchaseEntriesByIdResponse>> HandleAsync(UpdatePurchaseEntriesByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var purchase = await dbContext.Purchases
                .Include(p => p.Products)
                .ThenInclude(p => p.Product)
                .Include(p => p.Products)
                .ThenInclude(p => p.ProductPrice)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (purchase == null)
            {
                return PurchaseErrors.NotFound(command.Id);
            }

            var dtNow = dtProvider.UtcNow;

            var inputEntryIds = command.ProductEntries.Select(e => e.Id);

            var currentEntryIds = purchase.Products.Select(pe => pe.Id);
            var foreignEntryIds = inputEntryIds
                .Where(i => i.HasValue && !currentEntryIds.Contains(i.Value))
                .Cast<Guid>();

            if (foreignEntryIds.Any())
            {
                return PurchaseErrors.ContainForeignEntries(purchase.Id, foreignEntryIds);
            }

            var existingEntryIds = currentEntryIds.IntersectBy(inputEntryIds, i => i);

            var inputNewEntries = command.ProductEntries.Where(pe => !pe.Id.HasValue);
            var inputExistingEntries = command.ProductEntries.Where(pe => pe.Id.HasValue);

            var existingEntries = purchase.Products.Where(pe => existingEntryIds.Contains(pe.Id));

            var existingInputProductIds = existingEntries.Select(e => e.ProductId);
            var inputNewProductIds = inputNewEntries.Select(e => e.ProductId);

            var inputCombinedProductIds = existingInputProductIds.Concat(inputNewProductIds);
            var removedProducts = purchase.Products.Except(existingEntries);

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
                return PurchaseErrors.ContainNonExistingProducts(invalidProducts);
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
                dbContext.PurchaseProductEntries.RemoveRange(removedProducts);
            }

            if (inputNewEntries.Any())
            {
                var inputNewProducts = products.Where(p => inputNewProductIds.Contains(p.Id));

                var newProductEntries = inputNewProducts.Select(p => PurchaseProductEntry.CreateNew(
                    purchase.Id, p.Id, p.CurrentPrice!.Id,
                    inputNewEntries.Where(e => e.ProductId == p.Id).Sum(e => e.Quantity)
                ));

                dbContext.PurchaseProductEntries.AddRange(newProductEntries);
            }

            purchase.UpdateLastUpdatedTime(dtNow);

            await dbContext.SaveChangesAsync(cancellationToken);

            purchase.Products.Clear();
            (await dbContext.PurchaseProductEntries
                .Include(pe => pe.Product)
                .Include(pe => pe.ProductPrice)
                .AsNoTracking()
                .Where(pe => pe.PurchaseId == purchase.Id)
                .ToListAsync(cancellationToken)
            ).ForEach(purchase.Products.Add);

            return purchase.ToUpdateEntriesByIdResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating purchase's product entries " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdatePurchaseEntriesByIdCommandHandler),
                $"DB error has occurred while updating purchase's product entries with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdatePurchaseEntriesByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating purchase's product entries " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdatePurchaseEntriesByIdCommandHandler),
                "Unexpected error has occurred while updating purchase's product entries " +
                    $"with id '{command.Id}' to DB");
        }
    }
}