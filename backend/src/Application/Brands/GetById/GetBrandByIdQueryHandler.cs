using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Brands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Brands.GetById;

internal sealed class GetBrandByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetBrandByIdQueryHandler> logger
) : IQueryHandler<GetBrandByIdQuery, BrandResponse>
{
    public async Task<Result<BrandResponse>> HandleAsync(GetBrandByIdQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await dbContext.Brands
                .FirstOrDefaultAsync(b => b.Id == query.Id, cancellationToken);

            if (brand == null)
            {
                return BrandErrors.NotFound(query.Id);
            }

            return new BrandResponse(brand.Id, brand.Name, brand.CreatedTime);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetBrandByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting brand with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetBrandByIdQueryHandler),
                $"Unexpected error has occurred while getting brand with id '{query.Id}' from DB");
        }
    }
}