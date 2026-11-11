using Application.Sales.Create;
using Application.Sales.UpdateDetailsById;
using Application.Sales.UpdateEntriesById;
using Domain.Sales;

namespace Application.Sales;

public static class SaleMapper
{
    public static CreateSaleResponse ToCreateResponse(this Sale sale)
    {
        return new CreateSaleResponse(
            sale.Id,
            sale.CreatedTime
        );
    }

    public static SaleResponse ToSaleResponse(this Sale sale)
    {
        return new SaleResponse(
            sale.Id,
            sale.Title,
            sale.Tags.Select(t => t.Name.Value).ToArray(),
            sale.Products.Select(p => p.ToProductEntryResponse()).ToArray(),
            sale.Products.Sum(p => p.ProductPrice!.Value * p.Quantity),
            sale.OccurenceTime,
            sale.CreatedTime,
            sale.LastUpdatedTime
        );
    }

    public static ProductEntryResponse ToProductEntryResponse(this SaleProductEntry entry)
    {
        var priceValue = entry.ProductPrice!.Value;
        return new ProductEntryResponse(
            entry.Id, entry.ProductId, entry.Product!.Name,
            entry.PriceId, priceValue, entry.Quantity, priceValue * entry.Quantity
        );
    }

    public static UpdateSaleDetailsByIdResponse ToUpdateDetailByIdResponse(this Sale sale)
    {
        return new UpdateSaleDetailsByIdResponse(
            sale.Id,
            sale.Title,
            sale.Tags.Select(t => t.Name.Value).ToArray(),
            sale.OccurenceTime,
            sale.LastUpdatedTime
        );
    }

    public static UpdateSaleEntriesByIdResponse ToUpdateEntriesByIdResponse(this Sale sale)
    {
        return new UpdateSaleEntriesByIdResponse(
            sale.Id,
            sale.Products.Select(p => p.ToProductEntryResponse()).ToArray(),
            sale.Products.Sum(p => p.ProductPrice!.Value * p.Quantity),
            sale.LastUpdatedTime
        );
    }
}