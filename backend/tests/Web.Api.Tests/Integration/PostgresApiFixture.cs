using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;
using Testcontainers.PostgreSql;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Integration;

/// <summary>
/// Starts one PostgreSQL container and one API host for all integration tests. The host runs as Development, so it
/// applies the real migrations on startup. Tests share the database, so they use unique names rather than resetting it.
/// </summary>
public sealed class PostgresApiFixture : IAsyncLifetime
{
    public const string InitialUserEmail = "owner@example.com";
    public const string InitialUserPassword = "Own3rPass!";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ApiHost? host;

    public IServiceProvider Services => host!.Services;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        host = CreateHostOn(container.GetConnectionString(), InitialUserEmail, InitialUserPassword);
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

    public HttpClient CreateAnonymousClient()
    {
        return host!.CreateAnonymousClient();
    }

    /// <summary>Creates another host on the same container, using the shared database or a new one.</summary>
    public ApiHost CreateHost(string initialUserEmail, string initialUserPassword, string? database = null)
    {
        return CreateHostOn(ConnectionStringFor(database), initialUserEmail, initialUserPassword);
    }

    /// <summary>Returns a connection string to the shared database, or to another one on the same server.</summary>
    public string ConnectionStringFor(string? database = null)
    {
        var builder = new NpgsqlConnectionStringBuilder(container.GetConnectionString());
        if (database != null)
        {
            builder.Database = database;
        }

        return builder.ConnectionString;
    }

    public static string Unique(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private static ApiHost CreateHostOn(string connectionString, string initialUserEmail, string initialUserPassword)
    {
        return new ApiHost("Development", connectionString, new Dictionary<string, string?>
        {
            ["InitialUser:Email"] = initialUserEmail,
            ["InitialUser:Password"] = initialUserPassword,
            ["InitialUser:FirstName"] = "Owner",
        });
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
