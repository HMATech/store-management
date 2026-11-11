using Domain.Products;

namespace Domain.Stocks;

/// <summary>
/// A manual change to a product's stock that isn't a purchase or a sale, such as spoiled goods or a correction after
/// counting the shelf. A positive quantity adds stock and a negative one removes it.
/// </summary>
public sealed class StockAdjustment
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime OccurrenceTime { get; private set; }
    public DateTime CreatedTime { get; private set; }

    // navs
    public Product? Product { get; private set; }

    private StockAdjustment() { }

    public static StockAdjustment CreateNew(Guid productId, int quantity, string reason,
        DateTime occurrenceTime, DateTime createdTime)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = quantity,
            Reason = reason,
            OccurrenceTime = occurrenceTime,
            CreatedTime = createdTime,
        };
    }
}
