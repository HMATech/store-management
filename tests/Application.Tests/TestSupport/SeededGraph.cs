using Domain.Brands;
using Domain.Products;
using Domain.Purchases;
using Domain.Roles;
using Domain.Sales;
using Domain.Stocks;
using Domain.Users;

namespace Application.Tests.TestSupport;

/// <summary>A small, connected set of entities that handler tests can act on.</summary>
internal sealed class SeededGraph
{
    public const decimal CurrentPriceValue = 10m;
    public const decimal OldPriceValue = 8m;
    public const decimal OtherPriceValue = 4m;
    public const int EntryQuantity = 3;
    public const int AdjustmentQuantity = -1;

    public static readonly DateTime OccurredAt = new(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    public required Brand Brand { get; init; }
    public required ProductUnit Unit { get; init; }
    public required ProductUnit OtherUnit { get; init; }
    public required ProductTag Tag { get; init; }
    public required Product Product { get; init; }
    public required ProductPrice OldPrice { get; init; }
    public required ProductPrice CurrentPrice { get; init; }
    public required Product OtherProduct { get; init; }
    public required User User { get; init; }
    public required Role Role { get; init; }
    public required Role OtherRole { get; init; }
    public required PurchaseTag PurchaseTag { get; init; }
    public required Purchase Purchase { get; init; }
    public required PurchaseProductEntry PurchaseEntry { get; init; }
    public required SaleTag SaleTag { get; init; }
    public required Sale Sale { get; init; }
    public required SaleProductEntry SaleEntry { get; init; }
    public required StockAdjustment StockAdjustment { get; init; }

    public static async Task<SeededGraph> CreateAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var earlier = FixedClock.Now.AddDays(-30);

        var brand = Brand.CreateNew("Acme", earlier);
        var unit = ProductUnit.CreateNew("Litre");
        var otherUnit = ProductUnit.CreateNew("Gram");
        var tag = ProductTag.CreateNew("Dairy");
        var role = Role.CreateNew("Admin", "Administrators", earlier);
        var otherRole = Role.CreateNew("Cashier", null, earlier);
        context.AddRange(brand, unit, otherUnit, tag, role, otherRole);
        await context.SaveChangesAsync();

        var product = Product.CreateNew("Milk", "Fresh milk", brand.Id, 1.5f, unit.Id, earlier);
        var oldPrice = ProductPrice.CreateNew(product.Id, OldPriceValue, earlier, earlier, earlier.AddDays(1));
        var currentPrice = ProductPrice.CreateNew(product.Id, CurrentPriceValue,
            earlier.AddDays(1), earlier.AddDays(1), null);
        product.AddPrice(oldPrice);
        product.AddPrice(currentPrice);
        product.UpdateTags([tag]);

        var otherProduct = Product.CreateNew("Butter", null, brand.Id, 2f, otherUnit.Id, earlier);
        otherProduct.AddPrice(ProductPrice.CreateNew(otherProduct.Id, OtherPriceValue, earlier, earlier, null));

        var user = User.CreateNew("jane@example.com", "hashed:Passw0rd!", "Jane", "Doe", earlier);
        var userRole = UserRole.CreateNew(user.Id, role.Id, earlier);

        var purchaseTag = PurchaseTag.CreateNew("Stock");
        var purchase = Purchase.CreateNew("Weekly restock", OccurredAt, earlier);
        var purchaseEntry = PurchaseProductEntry.CreateNew(purchase.Id, product.Id, currentPrice.Id, EntryQuantity);
        purchase.UpdateProductEntries([purchaseEntry]);
        purchase.UpdateTags([purchaseTag]);

        var saleTag = SaleTag.CreateNew("Retail");
        var sale = Sale.CreateNew("Morning sale", OccurredAt, earlier);
        var saleEntry = SaleProductEntry.CreateNew(sale.Id, product.Id, currentPrice.Id, EntryQuantity);
        sale.UpdateProductEntries([saleEntry]);
        sale.UpdateTags([saleTag]);

        var stockAdjustment = StockAdjustment.CreateNew(product.Id, AdjustmentQuantity, "Spilled", OccurredAt, earlier);

        context.AddRange(product, otherProduct, user, userRole, purchase, sale, stockAdjustment);
        await context.SaveChangesAsync();

        return new SeededGraph
        {
            Brand = brand,
            Unit = unit,
            OtherUnit = otherUnit,
            Tag = tag,
            Product = product,
            OldPrice = oldPrice,
            CurrentPrice = currentPrice,
            OtherProduct = otherProduct,
            User = user,
            Role = role,
            OtherRole = otherRole,
            PurchaseTag = purchaseTag,
            Purchase = purchase,
            PurchaseEntry = purchaseEntry,
            SaleTag = saleTag,
            Sale = sale,
            SaleEntry = saleEntry,
            StockAdjustment = stockAdjustment,
        };
    }
}
