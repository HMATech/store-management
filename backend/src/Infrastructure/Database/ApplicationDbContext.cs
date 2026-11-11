using Domain.Products;
using Domain.Purchases;
using Domain.Roles;
using Domain.Sales;
using Domain.Stocks;
using Domain.Users;
using Domain.Common;
using Domain.Brands;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Application.Abstractions.Data;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ILogger<ApplicationDbContext> logger;

    public DbSet<User> Users { get; private set; }
    public DbSet<Role> Roles { get; private set; }
    public DbSet<UserRole> UserRoles { get; private set; }
    public DbSet<Product> Products { get; private set; }
    public DbSet<ProductDetail> ProductDetails { get; private set; }
    public DbSet<ProductPrice> ProductPrices { get; private set; }
    public DbSet<ProductUnit> ProductUnits { get; private set; }
    public DbSet<ProductTag> ProductTags { get; private set; }
    public DbSet<Brand> Brands { get; private set; }
    public DbSet<Purchase> Purchases { get; private set; }
    public DbSet<PurchaseProductEntry> PurchaseProductEntries { get; private set; }
    public DbSet<PurchaseTag> PurchaseTags { get; private set; }
    public DbSet<Sale> Sales { get; private set; }
    public DbSet<SaleProductEntry> SaleProductEntries { get; private set; }
    public DbSet<SaleTag> SaleTags { get; private set; }
    public DbSet<StockAdjustment> StockAdjustments { get; private set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
        ILogger<ApplicationDbContext> logger)
        : base(options)
    {
        this.logger = logger;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // modelBuilder.ConfigureSingularTableNaming(logger);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.HasDefaultSchema(Schemas.Default);

        modelBuilder.Owned<Name>();
        modelBuilder.ConfigureDateTime();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}