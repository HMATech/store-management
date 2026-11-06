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

            var productEntries = products.Select(p => PurchaseProductEntry.CreateNew(
                purchase.Id, p.Id, p.CurrentPrice!.Id,
                command.ProductEntries.Where(e => e.Id == p.Id).Sum(e => e.Quantity)
            ));

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