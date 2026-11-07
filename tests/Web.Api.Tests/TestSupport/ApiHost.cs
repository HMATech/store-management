using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Web.Api.Tests.TestSupport;

/// <summary>
/// Runs the real API in memory. This public wrapper is needed because Program is internal and xUnit requires test
/// classes, and therefore the types in their constructors, to be public.
/// </summary>
public sealed class ApiHost : IDisposable
{
    public const string JwtSecret = "test-secret-test-secret-test-secret-test-secret";
    public const string JwtIssuer = "test-issuer";
    public const string JwtAudience = "developer";

    private readonly WebApplicationFactory<Program> factory;

    public ApiHost(string environment, string connectionString,
        IReadOnlyDictionary<string, string?>? settings = null)
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:MainDb", connectionString);
            builder.UseSetting("Jwt:ValidIssuer", JwtIssuer);
            builder.UseSetting("Jwt:ValidAudiences:0", JwtAudience);
            builder.UseSetting("Jwt:Secret", JwtSecret);
            builder.UseSetting("Jwt:ExpirationInMinutes", "60");

            foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            {
                builder.UseSetting(key, value);
            }
        });
    }

    public IServiceProvider Services => factory.Services;

    /// <summary>Creates a client that sends a valid bearer token, as a signed-in user would.</summary>
    public HttpClient CreateClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create());
        return client;
    }

    public HttpClient CreateAnonymousClient()
    {
        return factory.CreateClient();
    }

    public void Dispose()
    {
        factory.Dispose();
    }
}

/// <summary>
/// Hosts the API with a connection string that refuses connections, for tests that must not reach a database.
/// </summary>
public sealed class NoDatabaseApiFixture : IDisposable
{
    private readonly ApiHost host = new("Testing", "Host=localhost;Port=1;Timeout=1;Pooling=false");

    public IServiceProvider Services => host.Services;

    public HttpClient CreateClient()
    {
        return host.CreateClient();
    }

    public HttpClient CreateAnonymousClient()
    {
        return host.CreateAnonymousClient();
    }

    public void Dispose()
    {
        host.Dispose();
    }
}
