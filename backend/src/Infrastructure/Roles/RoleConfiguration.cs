using Domain.Roles;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Roles;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("role");

        builder
            .OwnsOne(r => r.Name)
            .ConfigureNameProperty();
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder
            .HasOne(ur => ur.User)
            .WithOne()
            .HasForeignKey<UserRole>(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ur => ur.Role)
            .WithOne()
            .HasForeignKey<UserRole>(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ur => ur.UserId)
            .IsUnique(false);

        builder.HasIndex(ur => ur.RoleId)
            .IsUnique(false);

        builder.HasIndex(ur => new
        {
            ur.UserId,
            ur.RoleId
        })
            .IsUnique(true);
    }
}