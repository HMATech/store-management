using Domain.Brands;
using Domain.Products;
using Domain.Purchases;
using Domain.Roles;
using Domain.Sales;
using Domain.Stocks;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    public DbSet<User> Users { get; }
    public DbSet<Role> Roles { get; }
    public DbSet<UserRole> UserRoles { get; }
    public DbSet<Product> Products { get; }
    public DbSet<ProductDetail> ProductDetails { get; }
    public DbSet<ProductPrice> ProductPrices { get; }
    public DbSet<ProductUnit> ProductUnits { get; }
    public DbSet<ProductTag> ProductTags { get; }
    public DbSet<Brand> Brands { get; }
    public DbSet<Purchase> Purchases { get; }
    public DbSet<PurchaseProductEntry> PurchaseProductEntries { get; }
    public DbSet<PurchaseTag> PurchaseTags { get; }
    public DbSet<Sale> Sales { get; }
    public DbSet<SaleProductEntry> SaleProductEntries { get; }
    public DbSet<SaleTag> SaleTags { get; }
    public DbSet<StockAdjustment> StockAdjustments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}