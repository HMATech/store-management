using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Purchases.Create;

internal sealed class CreatePurchaseCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<CreatePurchaseCommandHandler> logger
) : ICommandHandler<CreatePurchaseCommand, CreatePurchaseResponse>
{
    public async Task<Result<CreatePurchaseResponse>> HandleAsync(CreatePurchaseCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var dtNow = dtProvider.UtcNow;

            var inputProductIds = command.ProductEntries.Select(e => e.Id).Distinct().ToArray();

            var productIds = await dbContext.Products
                .Where(p => inputProductIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToArrayAsync(cancellationToken);

            var invalidProducts = inputProductIds.Except(productIds);
            if (invalidProducts.Any())
            {
                return PurchaseErrors.ContainNonExistingProducts(invalidProducts);
            }

            var purchase = Purchase.CreateNew(command.Title, command.OccurrenceTime, dtNow);

            if (command.Tags.Length > 0)
            {
                IEnumerable<PurchaseTag> purchaseTags = [];

                purchaseTags = await TagsHelper.CreateSynchronizedTagsAsync(command.Tags, PurchaseTag.CreateNew, purchase.Tags,
                    dbContext.PurchaseTags, cancellationToken);
                
                purchase.UpdateTags(purchaseTags.ToList());
            }

            // A line is a product at a unit cost, so repeats of the same product and cost add together.
            var productEntries = command.ProductEntries
                .GroupBy(e => (e.Id, e.UnitCost))
                .Select(g => PurchaseProductEntry.CreateNew(purchase.Id, g.Key.Id, g.Key.UnitCost,
                    g.Sum(e => e.Quantity)));

            purchase.UpdateProductEntries(productEntries.ToList());
            dbContext.Purchases.Add(purchase);

            await dbContext.SaveChangesAsync(cancellationToken);

            return purchase.ToCreateResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while adding new purchase '{command.Title}' to DB",
                command.Title);
            return ApplicationErrors.DBOperationError(nameof(CreatePurchaseCommandHandler),
                $"DB error has occurred while adding new purchase '{command.Title}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(CreatePurchaseCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while adding new purchase '{command.Title}' to DB",
                command.Title);
            return ApplicationErrors.UnexpectedError(nameof(CreatePurchaseCommandHandler),
                $"Unexpected error has occurred while adding new purchase '{command.Title}' to DB");
        }
    }
}