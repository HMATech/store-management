using Domain.Sales;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Sales;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sale");
        
        builder
            .HasMany(s => s.Products)
            .WithOne(spe => spe.Sale)
            .HasForeignKey(spe => spe.SaleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(s => s.Tags)
            .WithMany(t => t.Sale);
    }
}

internal sealed class SaleTagConfiguration : IEntityTypeConfiguration<SaleTag>
{
    public void Configure(EntityTypeBuilder<SaleTag> builder)
    {
        builder
            .OwnsOne(pt => pt.Name)
            .ConfigureNameProperty();
    }
}

internal sealed class SaleProductEntryConfiguration : IEntityTypeConfiguration<SaleProductEntry>
{
    public void Configure(EntityTypeBuilder<SaleProductEntry> builder)
    {
        builder.ToTable("sale_product_entry");

        builder
            .HasOne(spe => spe.Product)
            .WithMany()
            .HasForeignKey(spe => spe.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(spe => spe.ProductPrice)
            .WithMany()
            .HasForeignKey(spe => spe.PriceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}