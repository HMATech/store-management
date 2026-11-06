using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Integration;

/// <summary>
/// Starts one PostgreSQL container and one API host for all integration tests. The host runs as Development, so it
/// applies the real migrations on startup. Tests share the database, so they use unique names rather than resetting it.
/// </summary>
public sealed class PostgresApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ApiHost? host;

    public IServiceProvider Services => host!.Services;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        host = new ApiHost("Development", container.GetConnectionString());
        _ = host.Services;
    }

    public async Task DisposeAsync()
    {
        host?.Dispose();
        await container.DisposeAsync();
    }

    public HttpClient CreateClient()
    {
        return host!.CreateClient();
    }

    public static string Unique(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresApiFixture>
{
    public const string Name = "Postgres";
}

internal static class HttpTestExtensions
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    public static string CreatedId(this HttpResponseMessage response)
    {
        return response.Headers.Location!.ToString().TrimEnd('/').Split('/')[^1];
    }

    public static void UseBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
