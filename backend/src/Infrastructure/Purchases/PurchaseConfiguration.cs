using Domain.Purchases;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Purchases;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchase");

        builder
            .HasMany(p => p.Products)
            .WithOne(pe => pe.Purchase)
            .HasForeignKey(pe => pe.PurchaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(p => p.Tags)
            .WithMany(t => t.Purchases);
    }
}

internal sealed class PurchaseTagConfiguration : IEntityTypeConfiguration<PurchaseTag>
{
    public void Configure(EntityTypeBuilder<PurchaseTag> builder)
    {
        builder
            .OwnsOne(pt => pt.Name)
            .ConfigureNameProperty();
    }
}

internal sealed class PurchaseProductEntryConfiguration : IEntityTypeConfiguration<PurchaseProductEntry>
{
    public void Configure(EntityTypeBuilder<PurchaseProductEntry> builder)
    {
        builder.ToTable("purchase_product_entry");

        builder
            .HasOne(pe => pe.Product)
            .WithMany()
            .HasForeignKey(spe => spe.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}