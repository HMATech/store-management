using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductPrices.GetById;

internal sealed class GetProductPriceByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductPriceByIdQueryHandler> logger
) : IQueryHandler<GetProductPriceByIdQuery, ProductPriceResponse>
{
    public async Task<Result<ProductPriceResponse>> HandleAsync(GetProductPriceByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var productPrice = await dbContext.ProductPrices
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            if (productPrice == null)
            {
                return ProductErrors.PriceNotFound(query.Id);
            }

            return productPrice.ToProductPriceResponse();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductPriceByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting product price " +
                "with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetProductPriceByIdQueryHandler),
                "Unexpected error has occurred while getting product price " +
                $"with id '{query.Id}' from DB");
        }
    }
}