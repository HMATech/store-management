using Application.Abstractions.Data;
using Domain.Products;

namespace Application.Stocks;

internal static class StockQueries
{
    /// <summary>
    /// Projects products to their stock, optionally keeping only one status. Quantities are summed as long so many
    /// large entries can't overflow, and the status filter runs in SQL so paging counts stay right.
    /// </summary>
    public static IQueryable<ProductStockResponse> SelectStock(this IQueryable<Product> products,
        IApplicationDbContext dbContext, StockStatus? status = null)
    {
        var stocks = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.StockDefinition.MinimumQuantity,
            Purchased = dbContext.PurchaseProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity),
            Sold = dbContext.SaleProductEntries.Where(e => e.ProductId == p.Id).Sum(e => (long)e.Quantity),
            Adjusted = dbContext.StockAdjustments.Where(a => a.ProductId == p.Id).Sum(a => (long)a.Quantity),
        });

        stocks = status switch
        {
            StockStatus.Negative => stocks.Where(s => s.Purchased - s.Sold + s.Adjusted < 0),
            StockStatus.Low => stocks.Where(s => s.Purchased - s.Sold + s.Adjusted >= 0
                && s.Purchased - s.Sold + s.Adjusted <= s.MinimumQuantity),
            StockStatus.Normal => stocks.Where(s => s.Purchased - s.Sold + s.Adjusted > s.MinimumQuantity),
            _ => stocks,
        };

        return stocks.Select(s => new ProductStockResponse(s.Id, s.Name, s.Purchased, s.Sold, s.Adjusted,
            s.MinimumQuantity));
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
    /// Decides a stock status. The status filter in <see cref="SelectStock"/> applies the same rules in SQL.
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
