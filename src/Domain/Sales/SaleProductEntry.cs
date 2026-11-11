using Domain.Common;
using Domain.Products;

namespace Domain.Sales;

public sealed class SaleProductEntry : ProductEntry
{
    public Guid SaleId { get; private set; }
    public Guid PriceId { get; private set; }

    // navs
    public Sale Sale { get; private set; } = default!;
    public ProductPrice? ProductPrice { get; private set; }

    private SaleProductEntry() { }

    public static SaleProductEntry CreateNew(Guid saleId, Guid productId, Guid priceId,
        int quantity)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            SaleId = saleId,
            ProductId = productId,
            PriceId = priceId,
            Quantity = quantity
        };
    }
}
