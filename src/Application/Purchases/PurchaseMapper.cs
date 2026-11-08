using Application.Purchases.Create;
using Application.Purchases.UpdateDetailsById;
using Application.Purchases.UpdateEntriesById;
using Domain.Purchases;

namespace Application.Purchases;

public static class PurchaseMapper
{
    public static CreatePurchaseResponse ToCreateResponse(this Purchase purchase)
    {
        return new CreatePurchaseResponse(
            purchase.Id,
            purchase.CreatedTime
        );
    }

    public static PurchaseResponse ToPurchaseResponse(this Purchase purchase)
    {
        return new PurchaseResponse(
            purchase.Id,
            purchase.Title,
            purchase.Tags.Select(t => t.Name.Value).ToArray(),
            purchase.Products.Select(p => p.ToProductEntryResponse()).ToArray(),
            purchase.Products.Sum(p => p.UnitCost * p.Quantity),
            purchase.OccurenceTime,
            purchase.CreatedTime,
            purchase.LastUpdatedTime
        );
    }

    public static ProductEntryResponse ToProductEntryResponse(this PurchaseProductEntry entry)
    {
        return new ProductEntryResponse(
            entry.Id, entry.ProductId, entry.Product!.Name,
            entry.UnitCost, entry.Quantity, entry.UnitCost * entry.Quantity
        );
    }

    public static UpdatePurchaseDetailsByIdResponse ToUpdateDetailByIdResponse(this Purchase purchase)
    {
        return new UpdatePurchaseDetailsByIdResponse(
            purchase.Id,
            purchase.Title,
            purchase.Tags.Select(t => t.Name.Value).ToArray(),
            purchase.OccurenceTime,
            purchase.LastUpdatedTime
        );
    }

    public static UpdatePurchaseEntriesByIdResponse ToUpdateEntriesByIdResponse(this Purchase purchase)
    {
        return new UpdatePurchaseEntriesByIdResponse(
            purchase.Id,
            purchase.Products.Select(p => p.ToProductEntryResponse()).ToArray(),
            purchase.Products.Sum(p => p.UnitCost * p.Quantity),
            purchase.LastUpdatedTime
        );
    }
}