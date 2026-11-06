using Domain.Brands;
using Domain.Common;
using Domain.Products;
using Domain.Purchases;
using Domain.Roles;
using Domain.Sales;
using Domain.Users;
using Infrastructure.Common;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Database;

public class ModelConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql("Host=unused")
        .UseSnakeCaseNamingConvention()
        .Options, NullLogger<ApplicationDbContext>.Instance);

    [Fact]
    public void Migrations_AreInSyncWithTheModel()
    {
        Assert.False(context.Database.HasPendingModelChanges(),
            "The EF model has changes that no migration covers. Add a migration.");
    }

    [Theory]
    [InlineData(typeof(User), "user")]
    [InlineData(typeof(Role), "role")]
    [InlineData(typeof(Product), "product")]
    [InlineData(typeof(ProductDetail), "product_detail")]
    [InlineData(typeof(ProductPrice), "product_price")]
    [InlineData(typeof(ProductTag), "product_tag")]
    [InlineData(typeof(ProductUnit), "product_unit")]
    [InlineData(typeof(Purchase), "purchase")]
    [InlineData(typeof(PurchaseProductEntry), "purchase_product_entry")]
    [InlineData(typeof(Sale), "sale")]
    [InlineData(typeof(SaleProductEntry), "sale_product_entry")]
    public void Entity_MapsToSingularSnakeCaseTable(Type entityType, string table)
    {
        Assert.Equal(table, context.Model.FindEntityType(entityType)!.GetTableName());
    }

    [Theory]
    [InlineData(typeof(ProductDetail), nameof(ProductDetail.BrandId), DeleteBehavior.Restrict)]
    [InlineData(typeof(ProductDetail), nameof(ProductDetail.MeasureUnitId), DeleteBehavior.Restrict)]
    [InlineData(typeof(ProductDetail), nameof(ProductDetail.ProductId), DeleteBehavior.Cascade)]
    [InlineData(typeof(ProductPrice), nameof(ProductPrice.ProductId), DeleteBehavior.Cascade)]
    [InlineData(typeof(PurchaseProductEntry), nameof(PurchaseProductEntry.ProductId), DeleteBehavior.Restrict)]
    [InlineData(typeof(PurchaseProductEntry), nameof(PurchaseProductEntry.PriceId), DeleteBehavior.Restrict)]
    [InlineData(typeof(PurchaseProductEntry), nameof(PurchaseProductEntry.PurchaseId), DeleteBehavior.Cascade)]
    [InlineData(typeof(SaleProductEntry), nameof(SaleProductEntry.ProductId), DeleteBehavior.Restrict)]
    [InlineData(typeof(SaleProductEntry), nameof(SaleProductEntry.PriceId), DeleteBehavior.Restrict)]
    [InlineData(typeof(SaleProductEntry), nameof(SaleProductEntry.SaleId), DeleteBehavior.Cascade)]
    [InlineData(typeof(UserRole), nameof(UserRole.UserId), DeleteBehavior.Cascade)]
    [InlineData(typeof(UserRole), nameof(UserRole.RoleId), DeleteBehavior.Cascade)]
    public void ForeignKey_HasExpectedDeleteBehavior(Type entityType, string property, DeleteBehavior expected)
    {
        var foreignKey = context.Model.FindEntityType(entityType)!.GetForeignKeys()
            .Single(fk => fk.Properties.Single().Name == property);

        Assert.Equal(expected, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void UserRole_IsUniquePerUserAndRole_ButNotPerUserOrRole()
    {
        var indexes = context.Model.FindEntityType(typeof(UserRole))!.GetIndexes().ToList();

        Assert.True(FindIndex(indexes, nameof(UserRole.UserId), nameof(UserRole.RoleId)).IsUnique);
        Assert.False(FindIndex(indexes, nameof(UserRole.UserId)).IsUnique);
        Assert.False(FindIndex(indexes, nameof(UserRole.RoleId)).IsUnique);
    }

    [Theory]
    [InlineData(typeof(Role))]
    [InlineData(typeof(ProductTag))]
    [InlineData(typeof(ProductUnit))]
    [InlineData(typeof(PurchaseTag))]
    [InlineData(typeof(SaleTag))]
    public void OwnedName_MapsToNameColumns(Type ownerType)
    {
        var name = context.Model.FindEntityType(ownerType)!.FindNavigation("Name")!.TargetEntityType;
        var table = StoreObjectIdentifier.Table(name.GetTableName()!, name.GetSchema());

        Assert.Equal("name", name.FindProperty(nameof(Name.Value))!.GetColumnName(table));
        Assert.Equal("normalized_name", name.FindProperty(nameof(Name.Normalized))!.GetColumnName(table));
        Assert.Contains(name.GetIndexes(), i => i.Properties.Single().Name == nameof(Name.Normalized));
    }

    [Fact]
    public void UserName_MapsToFirstAndLastNameColumns()
    {
        var name = context.Model.FindEntityType(typeof(User))!.FindNavigation("Name")!.TargetEntityType;
        var table = StoreObjectIdentifier.Table(name.GetTableName()!, name.GetSchema());

        Assert.Equal("first_name", name.FindProperty(nameof(UserName.First))!.GetColumnName(table));
        Assert.Equal("last_name", name.FindProperty(nameof(UserName.Last))!.GetColumnName(table));
    }

    [Fact]
    public void Model_UsesPublicSchema()
    {
        Assert.Equal("public", context.Model.FindEntityType(typeof(Brand))!.GetSchema());
    }

    [Fact]
    public void ConfigureSingularTableNaming_UsesClassNameForEntities_AndSkipsOwnedTypes()
    {
        var builder = new ModelBuilder();
        builder.Entity<Brand>();
        builder.Entity<Role>().OwnsOne(r => r.Name);

        builder.ConfigureSingularTableNaming(NullLogger.Instance);

        Assert.Equal(nameof(Brand), builder.Model.FindEntityType(typeof(Brand))!.GetTableName());
        Assert.Equal(nameof(Role), builder.Model.FindEntityType(typeof(Role))!.GetTableName());
    }

    public void Dispose()
    {
        context.Dispose();
    }

    private static IReadOnlyIndex FindIndex(IEnumerable<IReadOnlyIndex> indexes, params string[] properties)
    {
        return indexes.Single(i => i.Properties.Select(p => p.Name).SequenceEqual(properties));
    }
}
