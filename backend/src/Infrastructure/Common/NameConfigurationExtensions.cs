using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Common;

public static class NameConfigurationExtensions
{
    public static OwnedNavigationBuilder<TEntity, Name> ConfigureNameProperty<TEntity>(
        this OwnedNavigationBuilder<TEntity, Name> builder) where TEntity : class
    {
        builder
            .Property(n => n.Value).HasColumnName("name");

        builder
            .Property(n => n.Normalized).HasColumnName("normalized_name");

        builder
            .HasIndex(n => n.Normalized);

        return builder;
    }
}