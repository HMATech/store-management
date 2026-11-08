using Application.Abstractions.Data;
using Domain.Products;

namespace Application.Stocks;

internal static class StockQueries
{
    /// <summary>
    /// Projects products to their stock. Quantities are summed as long so many large entries can't overflow.
    /// </summary>
    public static IQueryable<ProductStockResponse> SelectStock(this IQueryable<Product> products,
        IApplicationDbContext dbContext, StockStatus? status = null)
    {
        return products
            .WhereStockStatus(dbContext, status)
            .Select(p => new ProductStockResponse(
                p.Id,
                p.Name,
                dbContext.PurchaseProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity),
                dbContext.SaleProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity),
                dbContext.StockAdjustments.Where(a => a.ProductId == p.Id).Sum(a => (long)a.Quantity),
                p.StockDefinition.MinimumQuantity));
    }

    /// <summary>
    /// Keeps only products whose stock has the given status, or all of them when it's null. Runs in SQL, so paging
    /// counts stay right. It applies the same rules as <see cref="StatusOf"/>; keep the two in step.
    /// </summary>
    public static IQueryable<Product> WhereStockStatus(this IQueryable<Product> products,
        IApplicationDbContext dbContext, StockStatus? status)
    {
        if (status == null)
        {
            return products;
        }

        var stocks = products.Select(p => new
        {
            p.Id,
            p.StockDefinition.MinimumQuantity,
            Quantity = dbContext.PurchaseProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity)
                - dbContext.SaleProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity)
                + dbContext.StockAdjustments.Where(a => a.ProductId == p.Id).Sum(a => (long)a.Quantity),
        });

        var matchingIds = status switch
        {
            StockStatus.Negative => stocks.Where(s => s.Quantity < 0),
            StockStatus.Low => stocks.Where(s => s.Quantity >= 0 && s.Quantity <= s.MinimumQuantity),
            _ => stocks.Where(s => s.Quantity > s.MinimumQuantity),
        };

        var ids = matchingIds.Select(s => s.Id);
        return products.Where(p => ids.Contains(p.Id));
    }

    /// <summary>Reads a status by name, ignoring case. Unlike <see cref="Enum.TryParse{TEnum}(string?, out TEnum)"/>,
    /// it rejects numbers.</summary>
    public static bool TryParseStatus(string? value, out StockStatus status)
    {
        var name = Enum.GetNames<StockStatus>()
            .FirstOrDefault(n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
        status = name == null ? default : Enum.Parse<StockStatus>(name);
        return name != null;
    }

    /// <summary>
    /// Decides a stock status. <see cref="WhereStockStatus"/> applies the same rules in SQL.
    /// </summary>
    public static StockStatus StatusOf(long quantity, int minimumQuantity)
    {
        if (quantity < 0)
        {
            return StockStatus.Negative;
        }

        return quantity <= minimumQuantity ? StockStatus.Low : StockStatus.Normal;
    }
}
