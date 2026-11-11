using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Stocks;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.GetById;

internal sealed class GetProductByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductByIdQueryHandler> logger
) : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var product = await dbContext.Products
                .Include(p => p.Detail)
                .ThenInclude(d => d.Brand)
                .Include(p => p.Detail)
                .ThenInclude(d => d.MeasureUnit)
                .Include(p => p.Tags)
                .Include(p => p.Prices.OrderByDescending(pr => pr.CreatedTime))
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            if (product == null)
            {
                return ProductErrors.NotFound(query.Id);
            }

            var stock = await dbContext.Products
                .Where(p => p.Id == query.Id)
                .SelectStock(dbContext)
                .SingleAsync(cancellationToken);

            return product.ToProductResponse(stock);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting product with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetProductByIdQueryHandler),
                $"Unexpected error has occurred while getting product with id '{query.Id}' from DB");
        }
    }
}