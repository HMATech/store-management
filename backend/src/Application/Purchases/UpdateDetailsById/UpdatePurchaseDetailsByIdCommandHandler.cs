using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Purchases.UpdateDetailsById;

internal sealed class UpdatePurchaseDetailsByIdCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<UpdatePurchaseDetailsByIdCommandHandler> logger
) : ICommandHandler<UpdatePurchaseDetailsByIdCommand, UpdatePurchaseDetailsByIdResponse>
{
    public async Task<Result<UpdatePurchaseDetailsByIdResponse>> HandleAsync(UpdatePurchaseDetailsByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var purchase = await dbContext.Purchases
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (purchase == null)
            {
                return PurchaseErrors.NotFound(command.Id);
            }

            var dtNow = dtProvider.UtcNow;

            var tags = await TagsHelper.CreateSynchronizedTagsAsync(command.Tags, PurchaseTag.CreateNew, purchase.Tags,
                dbContext.PurchaseTags, cancellationToken);

            purchase.UpdateTitle(command.Title);
            purchase.UpdateTags(tags.ToList());
            purchase.UpdateOccurrenceTime(command.OccurrenceTime);

            purchase.UpdateLastUpdatedTime(dtNow);

            await dbContext.SaveChangesAsync(cancellationToken);

            return purchase.ToUpdateDetailByIdResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating purchase details with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdatePurchaseDetailsByIdCommandHandler),
                $"DB error has occurred while updating purchase details with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdatePurchaseDetailsByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating purchase details " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdatePurchaseDetailsByIdCommandHandler),
                $"Unexpected error has occurred while updating purchase details with id '{command.Id}' to DB");
        }
    }
}