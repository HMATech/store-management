using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Stocks.GetByProductId;

internal sealed class GetStockByProductIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetStockByProductIdQueryHandler> logger
) : IQueryHandler<GetStockByProductIdQuery, ProductStockResponse>
{
    public async Task<Result<ProductStockResponse>> HandleAsync(GetStockByProductIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var stock = await dbContext.Products
                .Where(p => p.Id == query.ProductId)
                .SelectStock(dbContext)
                .FirstOrDefaultAsync(cancellationToken);

            if (stock == null)
            {
                return ProductErrors.NotFound(query.ProductId);
            }

            return stock;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetStockByProductIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting stock of product " +
                "with id '{query.ProductId}' from DB",
                query.ProductId);
            return ApplicationErrors.UnexpectedError(nameof(GetStockByProductIdQueryHandler),
                $"Unexpected error has occurred while getting stock of product with id '{query.ProductId}' from DB");
        }
    }
}
