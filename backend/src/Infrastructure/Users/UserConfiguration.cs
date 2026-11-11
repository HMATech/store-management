using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("user");

        var owned = builder.OwnsOne(u => u.Name);

        owned
            .Property(n => n.First).HasColumnName("first_name");

        owned
            .Property(n => n.Last).HasColumnName("last_name");
    }
}
