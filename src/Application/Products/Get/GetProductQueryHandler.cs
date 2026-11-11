using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Common;
using Application.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Products.Get;

internal sealed class GetProductQueryHandler(
    IApplicationDbContext dbContext,
    ILogger<GetProductQueryHandler> logger
) : IQueryHandler<GetProductQuery, PagedResponse<ProductResponse>>
{
    public async Task<Result<PagedResponse<ProductResponse>>> HandleAsync(GetProductQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            StockStatus? status = StockQueries.TryParseStatus(query.StockStatus, out var parsed) ? parsed : null;

            var filtered = dbContext.Products
                .Where(p => string.IsNullOrEmpty(query.Search) || p.Name.Contains(query.Search))
                .WhereStockStatus(dbContext, status);

            var totalCount = await filtered.CountAsync(cancellationToken);

            var ordered = query.SortOrder == SortOrder.ASC
                ? filtered.OrderBy(p => p.Name)
                : filtered.OrderByDescending(p => p.Name);

            var products = await ordered
                .Include(p => p.Detail)
                .ThenInclude(d => d.Brand)
                .Include(p => p.Detail)
                .ThenInclude(d => d.MeasureUnit)
                .Include(p => p.Tags)
                .Include(p => p.Prices.OrderByDescending(pr => pr.CreatedTime))
                .AsSplitQuery()
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // One query for the stock of every product on the page.
            var productIds = products.Select(p => p.Id).ToList();
            var stocks = await dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .SelectStock(dbContext)
                .ToDictionaryAsync(s => s.ProductId, cancellationToken);

            var responses = products.Select(p => p.ToProductResponse(stocks[p.Id])).ToList();

            return PagedResponse<ProductResponse>.Create(query.Page, query.PageSize, totalCount, responses);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "");
            return ApplicationErrors.OperationCancelledError(nameof(GetProductQueryHandler), ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error has occurred while querying products with '{query}'",
                query);
            return ApplicationErrors.UnexpectedError(nameof(GetProductQueryHandler),
                $"Unexpected error has occurred while querying products with '{query}'");
        }
    }
}