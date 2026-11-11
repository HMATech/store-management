using System.Net;
using System.Net.Http.Json;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Web.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class AuthorizationIntegrationTests(PostgresApiFixture api)
{
    private const string Password = "Passw0rd!";

    [Fact]
    public async Task InitialUser_CanLogInAndUseProtectedEndpoints()
    {
        var client = api.CreateAnonymousClient();

        var login = await client.PostAsJsonAsync("/users/login",
            new { email = PostgresApiFixture.InitialUserEmail, password = PostgresApiFixture.InitialUserPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.UseBearerToken((await login.ReadJsonAsync()).GetProperty("jwt").GetString()!);

        var brands = await client.GetAsync("/brands");

        Assert.Equal(HttpStatusCode.OK, brands.StatusCode);
    }

    [Fact]
    public async Task Register_WithoutToken_ReturnsUnauthorizedAndCreatesNoUser()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var response = await api.CreateAnonymousClient().PostAsJsonAsync("/users/register",
            new { email, password = Password, firstName = "Jane" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(await UserExistsAsync(email));
    }

    [Fact]
    public async Task Register_WithToken_CreatesUserThatCanLogIn()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var register = await api.CreateClient().PostAsJsonAsync("/users/register",
            new { email, password = Password, firstName = "Jane" });
        var login = await api.CreateAnonymousClient()
            .PostAsJsonAsync("/users/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Startup_WhenUsersExist_DoesNotCreateInitialUser()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        using (var host = api.CreateHost(email, Password))
        {
            _ = host.Services;
        }

        Assert.False(await UserExistsAsync(email));
    }

    [Fact]
    public async Task Startup_OnEmptyDatabase_CreatesInitialUserOnce()
    {
        var database = $"initial_{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@example.com";

        using (var first = api.CreateHost(email, Password, database))
        {
            _ = first.Services;
        }

        using var second = api.CreateHost(email, Password, database);
        using var scope = second.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(email, (await context.Users.SingleAsync()).Email);
    }

    [Fact]
    public void Startup_WithInvalidInitialUser_Fails()
    {
        using var host = api.CreateHost("not-an-email", "weak", $"initial_{Guid.NewGuid():N}");

        var exception = Record.Exception(() => host.Services);

        Assert.NotNull(exception);
        Assert.Contains("Could not create the initial user 'not-an-email'", exception.ToString());
    }

    [Fact]
    public async Task Swagger_InDevelopment_IsAvailableWithoutToken()
    {
        var response = await api.CreateAnonymousClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<bool> UserExistsAsync(string email)
    {
        using var scope = api.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Users.AnyAsync(u => u.Email == email);
    }
}
