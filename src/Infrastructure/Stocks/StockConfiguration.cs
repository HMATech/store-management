using Domain.Stocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Stocks;

internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("stock_adjustment");

        builder
            .HasOne(sa => sa.Product)
            .WithMany()
            .HasForeignKey(sa => sa.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(sa => sa.ProductId);
    }
}
