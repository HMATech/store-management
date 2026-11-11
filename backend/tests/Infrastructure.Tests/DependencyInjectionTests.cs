using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Infrastructure.Authentication;
using Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared;

namespace Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersServices()
    {
        using var provider = BuildProvider(Environments.Production);
        using var scope = provider.CreateScope();

        Assert.IsType<ApplicationDbContext>(scope.ServiceProvider.GetRequiredService<IApplicationDbContext>());
        Assert.IsType<PasswordHasher>(provider.GetRequiredService<IPasswordHasher>());
        Assert.IsType<TokenProvider>(provider.GetRequiredService<ITokenProvider>());
        Assert.NotNull(provider.GetRequiredService<IDateTimeProvider>());
    }

    [Fact]
    public void AddInfrastructure_ConfiguresJwtValidationFromConfiguration()
    {
        using var provider = BuildProvider(Environments.Production);

        var parameters = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters;

        Assert.Equal("issuer", parameters.ValidIssuer);
        Assert.Contains("audience", parameters.ValidAudiences);
        Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);
        Assert.True(parameters.ValidateAudience);
        Assert.True(parameters.ValidateIssuer);
        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.True(parameters.ValidateLifetime);
    }

    [Fact]
    public void AddInfrastructure_WithoutJwtSection_FailsWhenOptionsAreRead()
    {
        using var provider = BuildProvider(Environments.Production, new Dictionary<string, string?>
        {
            ["ConnectionStrings:MainDb"] = "Host=unused",
        });

        Assert.Throws<NullReferenceException>(() => provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme));
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Production", false)]
    public void AddInfrastructure_LogsSensitiveDataOnlyInDevelopment(string environment, bool expected)
    {
        using var provider = BuildProvider(environment);
        using var scope = provider.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
        var extension = options.Extensions
            .OfType<Microsoft.EntityFrameworkCore.Infrastructure.CoreOptionsExtension>()
            .Single();

        Assert.Equal(expected, extension.IsSensitiveDataLoggingEnabled);
        Assert.Equal(expected, extension.DetailedErrorsEnabled);
    }

    private static ServiceProvider BuildProvider(string environment, Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["ConnectionStrings:MainDb"] = "Host=unused",
                ["Jwt:ValidIssuer"] = "issuer",
                ["Jwt:ValidAudiences:0"] = "audience",
                ["Jwt:Secret"] = "unit-test-secret-unit-test-secret-unit-test-secret",
                ["Jwt:ExpirationInMinutes"] = "60",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration, new FakeEnvironment(environment));

        return services.BuildServiceProvider();
    }

    private sealed class FakeEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
