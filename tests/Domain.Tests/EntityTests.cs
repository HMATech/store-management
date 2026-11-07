using Domain.Brands;
using Domain.Common;
using Domain.Products;
using Domain.Purchases;
using Domain.Roles;
using Domain.Sales;
using Domain.Stocks;
using Domain.Users;

namespace Domain.Tests;

public class EntityTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Later = Now.AddHours(1);

    [Theory]
    [InlineData("Kilogram", "kilogram")]
    [InlineData("  MiXeD Case  ", "mixed case")]
    public void Name_CreateNew_NormalizesToTrimmedLowercase(string input, string expected)
    {
        var name = Name.CreateNew(input);

        Assert.Equal(input, name.Value);
        Assert.Equal(expected, name.Normalized);
    }

    [Fact]
    public void Brand_CreateNew_AndUpdateName()
    {
        var brand = Brand.CreateNew("Acme", Now);

        Assert.Equal("Acme", brand.Name);
        Assert.Equal(Now, brand.CreatedTime);

        brand.UpdateName("Other");

        Assert.Equal("Other", brand.Name);
    }

    [Fact]
    public void User_CreateNew_AssignsIdAndName()
    {
        var user = User.CreateNew("a@b.com", "hash", "First", "Last", Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("a@b.com", user.Email);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal("First", user.Name.First);
        Assert.Equal("Last", user.Name.Last);
        Assert.Equal(Now, user.CreatedTime);
    }

    [Fact]
    public void User_UpdateDetail_ChangesBothNames()
    {
        var user = User.CreateNew("a@b.com", "hash", "First", "Last", Now);

        user.UpdateDetail("New", null);

        Assert.Equal("New", user.Name.First);
        Assert.Null(user.Name.Last);
    }

    [Fact]
    public void Role_CreateNew_AndUpdates()
    {
        var role = Role.CreateNew("Admin", "desc", Now);

        Assert.Equal("Admin", role.Name.Value);
        Assert.Equal("admin", role.Name.Normalized);
        Assert.Equal("desc", role.Description);
        Assert.Equal(Now, role.CreatedTime);

        role.UpdateName("Owner");
        role.UpdateDescription(null);

        Assert.Equal("owner", role.Name.Normalized);
        Assert.Null(role.Description);
    }

    [Fact]
    public void UserRole_CreateNew_SetsAllFields()
    {
        var userId = Guid.NewGuid();

        var userRole = UserRole.CreateNew(userId, 7, Now);

        Assert.Equal(userId, userRole.UserId);
        Assert.Equal(7, userRole.RoleId);
        Assert.Equal(Now, userRole.AssignedTime);
    }

    [Fact]
    public void Product_CreateNew_CreatesDetailWithSameId()
    {
        var product = Product.CreateNew("Milk", "Fresh", 3, 1.5f, 4, Now);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Milk", product.Name);
        Assert.Equal(product.Id, product.Detail.ProductId);
        Assert.Equal("Fresh", product.Detail.Description);
        Assert.Equal(3, product.Detail.BrandId);
        Assert.Equal(1.5f, product.Detail.Measure);
        Assert.Equal(4, product.Detail.MeasureUnitId);
        Assert.Equal(Now, product.CreatedTime);
        Assert.Equal(Now, product.LastUpdatedTime);
    }

    [Fact]
    public void Product_Updates_ChangeState()
    {
        var product = Product.CreateNew("Milk", null, 1, 1f, 1, Now);
        var detail = ProductDetail.CreateNew(product.Id, "New", 2, 2f, 2);
        var tag = ProductTag.CreateNew("Dairy");
        var price = ProductPrice.CreateNew(product.Id, 5m, Now, Now, null);

        product.UpdateName("Oat Milk");
        product.UpdateDetail(detail);
        product.UpdateTags([tag]);
        product.AddPrice(price);
        product.UpdateLastUpdatedTime(Later);

        Assert.Equal("Oat Milk", product.Name);
        Assert.Same(detail, product.Detail);
        Assert.Same(tag, Assert.Single(product.Tags));
        Assert.Same(price, Assert.Single(product.Prices));
        Assert.Equal(Later, product.LastUpdatedTime);
    }

    [Fact]
    public void ProductDetail_UpdateMeasure_ChangesMeasureAndUnit()
    {
        var detail = ProductDetail.CreateNew(Guid.NewGuid(), null, 1, 1f, 1);
        var unit = ProductUnit.CreateNew("Litre");

        detail.UpdateMeasure(2.5f, unit);

        Assert.Equal(2.5f, detail.Measure);
        Assert.Same(unit, detail.MeasureUnit);
    }

    [Fact]
    public void ProductDetail_UpdateDescription_ChangesDescription()
    {
        var detail = ProductDetail.CreateNew(Guid.NewGuid(), "Old", 1, 1f, 1);

        detail.UpdateDescription("New");
        Assert.Equal("New", detail.Description);

        detail.UpdateDescription(null);
        Assert.Null(detail.Description);
    }

    [Fact]
    public void ProductPrice_CreateNew_GeneratesIdWhenNotGiven()
    {
        var productId = Guid.NewGuid();

        var price = ProductPrice.CreateNew(productId, 5m, Now, Now, null);

        Assert.NotEqual(Guid.Empty, price.Id);
        Assert.Equal(productId, price.ProductId);
        Assert.Equal(5m, price.Value);
        Assert.Equal(Now, price.CreatedTime);
        Assert.Equal(Now, price.ValidFromTime);
        Assert.Null(price.ValidToTime);
    }

    [Fact]
    public void ProductPrice_CreateNew_UsesGivenId()
    {
        var id = Guid.NewGuid();

        var price = ProductPrice.CreateNew(id, Guid.NewGuid(), 5m, Now, Now, Later);

        Assert.Equal(id, price.Id);
        Assert.Equal(Later, price.ValidToTime);
    }

    [Fact]
    public void ProductPrice_Updates_ChangeState()
    {
        var price = ProductPrice.CreateNew(Guid.NewGuid(), 5m, Now, Now, null);

        price.UpdateValue(6m);
        price.UpdateValidToTime(Later);

        Assert.Equal(6m, price.Value);
        Assert.Equal(Later, price.ValidToTime);
    }

    [Fact]
    public void ProductUnit_CreateNew_AndUpdateName()
    {
        var unit = ProductUnit.CreateNew("Litre");

        Assert.Equal("litre", unit.Name.Normalized);

        unit.UpdateName("Gram");

        Assert.Equal("gram", unit.Name.Normalized);
    }

    [Fact]
    public void StockAdjustment_CreateNew_SetsAllFields()
    {
        var productId = Guid.NewGuid();
        var occurred = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var created = occurred.AddHours(1);

        var adjustment = StockAdjustment.CreateNew(productId, -2, "Spoiled", occurred, created);

        Assert.NotEqual(Guid.Empty, adjustment.Id);
        Assert.Equal(productId, adjustment.ProductId);
        Assert.Equal(-2, adjustment.Quantity);
        Assert.Equal("Spoiled", adjustment.Reason);
        Assert.Equal(occurred, adjustment.OccurrenceTime);
        Assert.Equal(created, adjustment.CreatedTime);
        Assert.NotEqual(adjustment.Id, StockAdjustment.CreateNew(productId, 1, "x", occurred, created).Id);
    }

    [Fact]
    public void Tags_CreateNew_NormalizeName_AndUpdateName()
    {
        Tag[] tags = [ProductTag.CreateNew("Hot"), PurchaseTag.CreateNew("Hot"), SaleTag.CreateNew("Hot")];

        foreach (var tag in tags)
        {
            Assert.Equal("hot", tag.Name.Normalized);

            tag.UpdateName("Cold");

            Assert.Equal("Cold", tag.Name.Value);
        }
    }

    [Fact]
    public void Purchase_CreateNew_AndUpdates()
    {
        var purchase = Purchase.CreateNew("Weekly", Now, Now);
        var entry = PurchaseProductEntry.CreateNew(purchase.Id, Guid.NewGuid(), Guid.NewGuid(), 2);
        var tag = PurchaseTag.CreateNew("Stock");

        Assert.NotEqual(Guid.Empty, purchase.Id);
        Assert.Equal(Now, purchase.LastUpdatedTime);

        purchase.UpdateTitle("Monthly");
        purchase.UpdateTags([tag]);
        purchase.UpdateProductEntries([entry]);
        purchase.UpdateOccurrenceTime(Later);
        purchase.UpdateLastUpdatedTime(Later);

        Assert.Equal("Monthly", purchase.Title);
        Assert.Same(tag, Assert.Single(purchase.Tags));
        Assert.Same(entry, Assert.Single(purchase.Products));
        Assert.Equal(Later, purchase.OccurenceTime);
        Assert.Equal(Later, purchase.LastUpdatedTime);
    }

    [Fact]
    public void Sale_CreateNew_AndUpdates()
    {
        var sale = Sale.CreateNew("Morning", Now, Now);
        var entry = SaleProductEntry.CreateNew(sale.Id, Guid.NewGuid(), Guid.NewGuid(), 2);
        var tag = SaleTag.CreateNew("Retail");

        Assert.NotEqual(Guid.Empty, sale.Id);
        Assert.Equal(Now, sale.LastUpdatedTime);

        sale.UpdateTitle("Evening");
        sale.UpdateTags([tag]);
        sale.UpdateProductEntries([entry]);
        sale.UpdateOccurrenceTime(Later);
        sale.UpdateLastUpdatedTime(Later);

        Assert.Equal("Evening", sale.Title);
        Assert.Same(tag, Assert.Single(sale.Tags));
        Assert.Same(entry, Assert.Single(sale.Products));
        Assert.Equal(Later, sale.OccurenceTime);
        Assert.Equal(Later, sale.LastUpdatedTime);
    }

    [Fact]
    public void ProductEntries_CreateNew_AndUpdateQuantity()
    {
        var parentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var priceId = Guid.NewGuid();

        var purchaseEntry = PurchaseProductEntry.CreateNew(parentId, productId, priceId, 2);
        var saleEntry = SaleProductEntry.CreateNew(parentId, productId, priceId, 2);

        Assert.Equal(parentId, purchaseEntry.PurchaseId);
        Assert.Equal(parentId, saleEntry.SaleId);

        foreach (var entry in new ProductEntry[] { purchaseEntry, saleEntry })
        {
            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.Equal(productId, entry.ProductId);
            Assert.Equal(priceId, entry.PriceId);
            Assert.Equal(2, entry.Quantity);

            entry.UpdateQuantity(5);

            Assert.Equal(5, entry.Quantity);
        }
    }
}
