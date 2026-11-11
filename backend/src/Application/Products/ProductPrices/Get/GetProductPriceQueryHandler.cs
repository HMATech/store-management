using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.ProductPrices.Get;

internal sealed class GetProductPriceQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductPriceQueryHandler> logger
) : IQueryHandler<GetProductPriceQuery, PagedResponse<ProductPriceResponse>>
{
    public async Task<Result<PagedResponse<ProductPriceResponse>>> HandleAsync(GetProductPriceQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var querySearch = string.IsNullOrEmpty(query.Search) ? "" : query.Search.ToLower();
            var result = dbContext.ProductPrices
                .Where(pp => pp.Product!.Name.ToLower().Contains(querySearch)
                    || pp.Product.Detail.Brand!.Name.ToLower().Contains(querySearch))
                .Select(pp => new
                {
                    Id = pp.Id,
                    ProductId = pp.Product!.Id,
                    Value = pp.Value,
                    CreatedTime = pp.CreatedTime,
                    ValidFromTime = pp.ValidFromTime,
                    ValidToTime = pp.ValidToTime
                })
                .AsQueryable();

            result = query.SortOrder == SortOrder.ASC
                ? result.OrderBy(p => p.ValidFromTime)
                : result.OrderByDescending(p => p.ValidFromTime);

            var responses = result
                .Select(p => ProductPrice.CreateNew(
                    p.Id, p.ProductId, p.Value, p.CreatedTime, p.ValidFromTime, p.ValidToTime
                ))
                .Select(p => p.ToProductPriceResponse());

            return await PagedResponse<ProductPriceResponse>.CreateAsync(responses, query.Page, query.PageSize);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductPriceQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying product prices with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetProductPriceQueryHandler),
                $"Unexpected error has occurred while querying product prices with '{query}'");
        }
    }
}