using Application.Abstractions.Data;
using Domain.Brands;
using Domain.Products;
using Domain.Purchases;
using Domain.Roles;
using Domain.Sales;
using Domain.Stocks;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.TestSupport;

/// <summary>Reads from a real context, but throws the given exception when changes are saved.</summary>
internal sealed class SaveFailingDbContext(ApplicationDbContext inner, Exception exception) : IApplicationDbContext
{
    public DbSet<User> Users => inner.Users;
    public DbSet<Role> Roles => inner.Roles;
    public DbSet<UserRole> UserRoles => inner.UserRoles;
    public DbSet<Product> Products => inner.Products;
    public DbSet<ProductDetail> ProductDetails => inner.ProductDetails;
    public DbSet<ProductPrice> ProductPrices => inner.ProductPrices;
    public DbSet<ProductUnit> ProductUnits => inner.ProductUnits;
    public DbSet<ProductTag> ProductTags => inner.ProductTags;
    public DbSet<Brand> Brands => inner.Brands;
    public DbSet<Purchase> Purchases => inner.Purchases;
    public DbSet<PurchaseProductEntry> PurchaseProductEntries => inner.PurchaseProductEntries;
    public DbSet<PurchaseTag> PurchaseTags => inner.PurchaseTags;
    public DbSet<Sale> Sales => inner.Sales;
    public DbSet<SaleProductEntry> SaleProductEntries => inner.SaleProductEntries;
    public DbSet<SaleTag> SaleTags => inner.SaleTags;
    public DbSet<StockAdjustment> StockAdjustments => inner.StockAdjustments;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw exception;
    }
}

/// <summary>Throws the given exception as soon as any table is accessed.</summary>
internal sealed class ThrowingDbContext(Exception exception) : IApplicationDbContext
{
    public DbSet<User> Users => throw exception;
    public DbSet<Role> Roles => throw exception;
    public DbSet<UserRole> UserRoles => throw exception;
    public DbSet<Product> Products => throw exception;
    public DbSet<ProductDetail> ProductDetails => throw exception;
    public DbSet<ProductPrice> ProductPrices => throw exception;
    public DbSet<ProductUnit> ProductUnits => throw exception;
    public DbSet<ProductTag> ProductTags => throw exception;
    public DbSet<Brand> Brands => throw exception;
    public DbSet<Purchase> Purchases => throw exception;
    public DbSet<PurchaseProductEntry> PurchaseProductEntries => throw exception;
    public DbSet<PurchaseTag> PurchaseTags => throw exception;
    public DbSet<Sale> Sales => throw exception;
    public DbSet<SaleProductEntry> SaleProductEntries => throw exception;
    public DbSet<SaleTag> SaleTags => throw exception;
    public DbSet<StockAdjustment> StockAdjustments => throw exception;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw exception;
    }
}
