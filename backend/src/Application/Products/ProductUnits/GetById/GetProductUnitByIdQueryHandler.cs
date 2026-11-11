using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductUnits.GetById;

internal sealed class GetProductUnitByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductUnitByIdQueryHandler> logger
) : IQueryHandler<GetProductUnitByIdQuery, ProductUnitResponse>
{
    public async Task<Result<ProductUnitResponse>> HandleAsync(GetProductUnitByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var productUnit = await dbContext.ProductUnits
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            if (productUnit == null)
            {
                return ProductErrors.UnitNotFound(query.Id);
            }

            return productUnit.ToProductUnitResponse();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductUnitByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting product unit " +
                "with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetProductUnitByIdQueryHandler),
                "Unexpected error has occurred while getting product unit " +
                $"with id '{query.Id}' from DB");
        }
    }
}