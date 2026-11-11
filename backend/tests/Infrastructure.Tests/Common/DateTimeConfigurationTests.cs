using Domain.Common;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Common;

public class DateTimeConfigurationTests
{
    private static readonly DateTime Utc = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Converter_ToProvider_ConvertsLocalTimeToUtc()
    {
        var local = Utc.ToLocalTime();

        var stored = (DateTime)new DateTimeUtcConverter().ConvertToProvider(local)!;

        Assert.Equal(Utc, stored);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
    }

    [Fact]
    public void Converter_FromProvider_MarksValueAsUtc()
    {
        var unspecified = DateTime.SpecifyKind(Utc, DateTimeKind.Unspecified);

        var read = (DateTime)new DateTimeUtcConverter().ConvertFromProvider(unspecified)!;

        Assert.Equal(DateTimeKind.Utc, read.Kind);
        Assert.Equal(Utc.Ticks, read.Ticks);
    }

    [Fact]
    public void NullableConverter_KeepsNull()
    {
        var converter = new NullableDateTimeUtcConverter();

        Assert.Null(converter.ConvertToProviderTyped(null));
        Assert.Null(converter.ConvertFromProviderTyped(null));
    }

    [Fact]
    public void NullableConverter_ConvertsValues()
    {
        var converter = new NullableDateTimeUtcConverter();

        Assert.Equal(Utc, converter.ConvertToProviderTyped(Utc.ToLocalTime()));
        Assert.Equal(DateTimeKind.Utc,
            converter.ConvertFromProviderTyped(DateTime.SpecifyKind(Utc, DateTimeKind.Unspecified))!.Value.Kind);
    }

    [Fact]
    public void ConfigureDateTime_AppliesConvertersByPropertyType()
    {
        using var context = new TimesContext();
        var entity = context.Model.FindEntityType(typeof(Times))!;

        Assert.IsType<DateTimeUtcConverter>(entity.FindProperty(nameof(Times.Required))!.GetValueConverter());
        Assert.IsType<NullableDateTimeUtcConverter>(entity.FindProperty(nameof(Times.Optional))!.GetValueConverter());
    }

    [Fact]
    public void ConfigureDateTime_SkipsNotUtcAndNonDateProperties()
    {
        using var context = new TimesContext();
        var entity = context.Model.FindEntityType(typeof(Times))!;

        Assert.Null(entity.FindProperty(nameof(Times.Local))!.GetValueConverter());
        Assert.Null(entity.FindProperty(nameof(Times.Id))!.GetValueConverter());
    }

    [Fact]
    public void DateTimeProvider_ReturnsCurrentUtcTime()
    {
        var before = DateTime.UtcNow;

        var now = new DateTimeProvider().UtcNow;

        Assert.Equal(DateTimeKind.Utc, now.Kind);
        Assert.InRange(now, before, DateTime.UtcNow);
    }

    private sealed class Times
    {
        public int Id { get; set; }
        public DateTime Required { get; set; }
        public DateTime? Optional { get; set; }

        [NotUtc]
        public DateTime Local { get; set; }
    }

    private sealed class TimesContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Times>();
            modelBuilder.ConfigureDateTime();
        }
    }
}
