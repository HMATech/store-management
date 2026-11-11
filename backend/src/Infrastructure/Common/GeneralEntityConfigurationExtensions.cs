using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Common;

public static class GeneralEntityConfigurationExtensions
{
    public static ModelBuilder ConfigureSingularTableNaming(this ModelBuilder modelBuilder, ILogger logger)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            logger.LogInformation("Entity name {name}, type {type}, shared {shr}", entity.ClrType.Name, entity.ClrType, entity.HasSharedClrType);
            if (entity.HasSharedClrType
                || entity.IsOwned()) continue;
            modelBuilder
                .Entity(entity.ClrType)
                .ToTable(entity.ClrType.Name);
        }

        return modelBuilder;
    }
}