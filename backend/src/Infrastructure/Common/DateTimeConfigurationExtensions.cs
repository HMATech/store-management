using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared;

namespace Infrastructure.Common;

public static class DateTimeConfigurationExtensions
{
    public static ModelBuilder ConfigureDateTime(this ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties()))
        {
            if (property.ClrType != typeof(DateTime)
                && property.ClrType != typeof(DateTime?)
                || property.PropertyInfo == null
                || property.PropertyInfo.GetCustomAttributes(true).Any(attr => attr is NotUtcAttribute))
            {
                continue;
            }

            if (property.ClrType == typeof(DateTime))
            {
                property.SetValueConverter(typeof(DateTimeUtcConverter));
                continue;
            }

            property.SetValueConverter(typeof(NullableDateTimeUtcConverter));
        }

        return modelBuilder;
    }
}

internal sealed class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUtcConverter() : base(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {

    }
}

internal sealed class NullableDateTimeUtcConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableDateTimeUtcConverter() : base(
        v => v.HasValue ? v.Value.ToUniversalTime() : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {

    }
}

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}