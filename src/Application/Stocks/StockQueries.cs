using Application.Abstractions.Data;
using Domain.Products;

namespace Application.Stocks;

internal static class StockQueries
{
    /// <summary>
    /// Projects products to their stock. Quantities are summed as long so many large entries can't overflow.
    /// </summary>
    public static IQueryable<ProductStockResponse> SelectStock(this IQueryable<Product> products,
        IApplicationDbContext dbContext)
    {
        return products.Select(p => new ProductStockResponse(
            p.Id,
            p.Name,
            dbContext.PurchaseProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity),
            dbContext.SaleProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity),
            dbContext.StockAdjustments.Where(a => a.ProductId == p.Id).Sum(a => (long)a.Quantity)));
    }
}
