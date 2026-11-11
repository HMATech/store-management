using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductTags.GetById;

internal sealed class GetProductTagByIdQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductTagByIdQueryHandler> logger
) : IQueryHandler<GetProductTagByIdQuery, ProductTagResponse>
{
    public async Task<Result<ProductTagResponse>> HandleAsync(GetProductTagByIdQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var productTag = await dbContext.ProductTags
                .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

            if (productTag == null)
            {
                return ProductErrors.TagNotFound(query.Id);
            }

            return productTag.ToProductTagResponse();
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductTagByIdQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while getting product tag " +
                "with id '{query.Id}' from DB",
                query.Id);
            return ApplicationErrors.UnexpectedError(nameof(GetProductTagByIdQueryHandler),
                "Unexpected error has occurred while getting product tag " +
                $"with id '{query.Id}' from DB");
        }
    }
}