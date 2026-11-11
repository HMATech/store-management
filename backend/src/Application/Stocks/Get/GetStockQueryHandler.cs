using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Stocks.Get;

internal sealed class GetStockQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetStockQueryHandler> logger
) : IQueryHandler<GetStockQuery, PagedResponse<ProductStockResponse>>
{
    public async Task<Result<PagedResponse<ProductStockResponse>>> HandleAsync(GetStockQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var products = dbContext.Products
                .Where(p => string.IsNullOrEmpty(query.Search) || p.Name.Contains(query.Search));

            products = query.SortOrder == SortOrder.ASC
                ? products.OrderBy(p => p.Name)
                : products.OrderByDescending(p => p.Name);

            StockStatus? status = StockQueries.TryParseStatus(query.Status, out var parsed) ? parsed : null;

            return await PagedResponse<ProductStockResponse>.CreateAsync(products.SelectStock(dbContext, status),
                query.Page, query.PageSize);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetStockQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying stock with '{query}'", query);
            return ApplicationErrors.UnexpectedError(nameof(GetStockQueryHandler),
                $"Unexpected error has occurred while querying stock with '{query}'");
        }
    }
}
