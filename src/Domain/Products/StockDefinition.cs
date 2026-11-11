namespace Domain.Products;

/// <summary>
/// The rules for judging a product's stock. Each product has one, set when the product is added.
/// <see cref="MinimumQuantity"/> is the first rule; add new rules here as further properties.
/// </summary>
public sealed class StockDefinition
{
    /// <summary>Used when a product is added without a minimum quantity.</summary>
    public const int DefaultMinimumQuantity = 5;

    /// <summary>Stock at or below this is running low.</summary>
    public int MinimumQuantity { get; private set; }

    private StockDefinition() { }

    public static StockDefinition CreateNew(int? minimumQuantity)
    {
        return new()
        {
            MinimumQuantity = minimumQuantity ?? DefaultMinimumQuantity,
        };
    }
}
