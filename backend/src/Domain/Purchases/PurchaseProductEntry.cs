using Domain.Common;

namespace Domain.Purchases;

public sealed class PurchaseProductEntry : ProductEntry
{
    public Guid PurchaseId { get; private set; }

    /// <summary>The price paid to the supplier for one unit, which can differ from the selling price.</summary>
    public decimal UnitCost { get; private set; }

    // navs
    public Purchase Purchase { get; private set; } = default!;

    private PurchaseProductEntry() { }

    public void UpdateUnitCost(decimal unitCost)
    {
        UnitCost = unitCost;
    }

    public static PurchaseProductEntry CreateNew(Guid purchaseId, Guid productId, decimal unitCost,
        int quantity)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            PurchaseId = purchaseId,
            ProductId = productId,
            UnitCost = unitCost,
            Quantity = quantity
        };
    }
}
