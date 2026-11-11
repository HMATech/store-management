using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Sales.GetById;

internal sealed class GetSaleByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetSaleByIdQueryHandler> logger
) : IQueryHandler<GetSaleByIdQuery, SaleResponse>
{
    public async Task<Result<SaleResponse>> HandleAsync(GetSaleByIdQuery query,
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
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            if (sale == null)
            {
                return SaleErrors.NotFound(query.Id);
            }

            return sale.ToSaleResponse();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetSaleByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting sale with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetSaleByIdQueryHandler),
                $"Unexpected error has occurred while getting sale with id '{query.Id}' from DB");
        }
    }
}
