using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Sales.UpdateDetailsById;

internal sealed class UpdateSaleDetailsByIdCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dtProvider,
    ILogger<UpdateSaleDetailsByIdCommandHandler> logger
) : ICommandHandler<UpdateSaleDetailsByIdCommand, UpdateSaleDetailsByIdResponse>
{
    public async Task<Result<UpdateSaleDetailsByIdResponse>> HandleAsync(UpdateSaleDetailsByIdCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var sale = await dbContext.Sales
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

            if (sale == null)
            {
                return SaleErrors.NotFound(command.Id);
            }

            var dtNow = dtProvider.UtcNow;

            var tags = await TagsHelper.CreateSynchronizedTagsAsync(command.Tags, SaleTag.CreateNew, sale.Tags,
                dbContext.SaleTags, cancellationToken);

            sale.UpdateTitle(command.Title);
            sale.UpdateTags(tags.ToList());
            sale.UpdateOccurrenceTime(command.OccurrenceTime);

            sale.UpdateLastUpdatedTime(dtNow);

            await dbContext.SaveChangesAsync(cancellationToken);

            return sale.ToUpdateDetailByIdResponse();
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "DB error has occurred while updating sale details with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.DBOperationError(nameof(UpdateSaleDetailsByIdCommandHandler),
                $"DB error has occurred while updating sale details with id '{command.Id}' to DB");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(UpdateSaleDetailsByIdCommandHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while updating sale details " +
                "with id '{command.Id}' to DB",
                command.Id);
            return ApplicationErrors.UnexpectedError(nameof(UpdateSaleDetailsByIdCommandHandler),
                $"Unexpected error has occurred while updating sale details with id '{command.Id}' to DB");
        }
    }
}