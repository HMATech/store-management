using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Purchases.GetById;

internal sealed class GetPurchaseByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetPurchaseByIdQueryHandler> logger
) : IQueryHandler<GetPurchaseByIdQuery, PurchaseResponse>
{
    public async Task<Result<PurchaseResponse>> HandleAsync(GetPurchaseByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var purchase = await dbContext.Purchases
                .Include(p => p.Products)
                .ThenInclude(p => p.Product)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            if (purchase == null)
            {
                return PurchaseErrors.NotFound(query.Id);
            }

            return purchase.ToPurchaseResponse();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetPurchaseByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting purchase with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetPurchaseByIdQueryHandler),
                $"Unexpected error has occurred while getting purchase with id '{query.Id}' from DB");
        }
    }
}
