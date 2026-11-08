using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Purchases.UpdateEntriesById;

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
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (purchase == null)
            {
                return PurchaseErrors.NotFound(command.Id);
            }

            var currentEntryIds = purchase.Products.Select(pe => pe.Id).ToHashSet();
            var foreignEntryIds = command.ProductEntries
                .Where(e => e.Id.HasValue && !currentEntryIds.Contains(e.Id.Value))
                .Select(e => e.Id!.Value)
                .Distinct()
                .ToArray();

            if (foreignEntryIds.Length > 0)
            {
                return PurchaseErrors.ContainForeignEntries(purchase.Id, foreignEntryIds);
            }

            var inputNewEntries = command.ProductEntries.Where(e => !e.Id.HasValue).ToArray();
            var inputNewProductIds = inputNewEntries.Select(e => e.ProductId).Distinct().ToArray();

            var existingProductIds = await dbContext.Products
                .Where(p => inputNewProductIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToArrayAsync(cancellationToken);

            var invalidProducts = inputNewProductIds.Except(existingProductIds).ToArray();
            if (invalidProducts.Length > 0)
            {
                return PurchaseErrors.ContainNonExistingProducts(invalidProducts);
            }

            var keptEntries = UpdateKeptEntries(purchase, command.ProductEntries);

            var removedEntries = purchase.Products.Where(pe => !keptEntries.Contains(pe)).ToArray();
            if (removedEntries.Length > 0)
            {
                dbContext.PurchaseProductEntries.RemoveRange(removedEntries);
            }

            AddNewEntries(purchase, keptEntries, inputNewEntries);

            purchase.UpdateLastUpdatedTime(dtProvider.UtcNow);

            await dbContext.SaveChangesAsync(cancellationToken);

            purchase.Products.Clear();
            (await dbContext.PurchaseProductEntries
                .Include(pe => pe.Product)
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

    /// <summary>
    /// A line is a product at a unit cost, so new units join a kept line with the same product and cost,
    /// and otherwise become a new line.
    /// </summary>
    private void AddNewEntries(Purchase purchase, List<PurchaseProductEntry> keptEntries,
        IEnumerable<ProductEntryCommand> inputNewEntries)
    {
        var newQuantities = inputNewEntries
            .GroupBy(e => (e.ProductId, e.UnitCost))
            .Select(g => (g.Key.ProductId, g.Key.UnitCost, Quantity: g.Sum(e => e.Quantity)));

        foreach (var (productId, unitCost, quantity) in newQuantities)
        {
            var matchingLine = keptEntries.FirstOrDefault(e => e.ProductId == productId && e.UnitCost == unitCost);
            if (matchingLine != null)
            {
                matchingLine.UpdateQuantity(matchingLine.Quantity + quantity);
            }
            else
            {
                dbContext.PurchaseProductEntries.Add(
                    PurchaseProductEntry.CreateNew(purchase.Id, productId, unitCost, quantity));
            }
        }
    }

    /// <summary>
    /// Sets the quantity and unit cost of every line the request keeps. A line listed more than once
    /// gets the sum of its quantities.
    /// </summary>
    private static List<PurchaseProductEntry> UpdateKeptEntries(Purchase purchase,
        IEnumerable<ProductEntryCommand> inputEntries)
    {
        var inputByEntryId = inputEntries
            .Where(e => e.Id.HasValue)
            .GroupBy(e => e.Id!.Value)
            .ToDictionary(g => g.Key, g => (Quantity: g.Sum(e => e.Quantity), g.First().UnitCost));

        var keptEntries = new List<PurchaseProductEntry>();
        foreach (var entry in purchase.Products)
        {
            if (inputByEntryId.TryGetValue(entry.Id, out var input))
            {
                entry.UpdateQuantity(input.Quantity);
                entry.UpdateUnitCost(input.UnitCost);
                keptEntries.Add(entry);
            }
        }

        return keptEntries;
    }
}
