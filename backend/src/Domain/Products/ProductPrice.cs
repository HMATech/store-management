namespace Domain.Products;

public sealed class ProductPrice
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Value { get; private set; }
    public DateTime CreatedTime { get; private set; }
    public DateTime ValidFromTime { get; private set; }
    public DateTime? ValidToTime { get; private set; }

    // navs
    public Product? Product { get; private set; } = default!;

    private ProductPrice() { }

    public void UpdateValue(decimal value)
    {
        Value = value;
    }

    public void UpdateValidToTime(DateTime validTo)
    {
        ValidToTime = validTo;
    }

    public static ProductPrice CreateNew(Guid productId, decimal value,
        DateTime createdTime, DateTime validFrom, DateTime? validTo)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Value = value,
            CreatedTime = createdTime,
            ValidFromTime = validFrom,
            ValidToTime = validTo
        };
    }

    public static ProductPrice CreateNew(Guid id, Guid productId, decimal value,
        DateTime createdTime, DateTime validFrom, DateTime? validTo)
    {
        return new()
        {
            Id = id,
            ProductId = productId,
            Value = value,
            CreatedTime = createdTime,
            ValidFromTime = validFrom,
            ValidToTime = validTo
        };
    }
}