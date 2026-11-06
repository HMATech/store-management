using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Infrastructure.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Web.Api.Infrastructure;

namespace Web.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class UserAndRoleIntegrationTests(PostgresApiFixture api)
{
    private const string Password = "Passw0rd!";

    private readonly HttpClient client = api.CreateClient();

    [Fact]
    public async Task Startup_AppliesAllMigrations()
    {
        using var scope = api.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task RegisterAndLogin_GivesTokenThatUnlocksProtectedEndpoints()
    {
        var email = await RegisterAsync();
        var login = await LoginAsync(email);
        client.UseBearerToken(login.GetProperty("jwt").GetString()!);
        var userId = login.GetProperty("id").GetString();

        var list = await client.GetAsync($"/users?search=Jane&pageSize=1");
        var byId = await client.GetAsync($"/users/{userId}");

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(email, (await byId.ReadJsonAsync()).GetProperty("email").GetString());
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var email = await RegisterAsync();

        var response = await client.PostAsJsonAsync("/users/register",
            new { email, password = Password, firstName = "Jane" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequestWithoutToken()
    {
        var email = await RegisterAsync();

        var response = await client.PostAsJsonAsync("/users/login", new { email, password = "Wrong0ne!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Users.IncorrectCredential", (await response.ReadJsonAsync()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        var response = await client.PostAsJsonAsync("/users/login",
            new { email = $"{Guid.NewGuid():N}@example.com", password = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Users.IncorrectCredential", (await response.ReadJsonAsync()).GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateAndDeleteUser_ChangeStoredData()
    {
        var login = await LoginAsync(await RegisterAsync());
        client.UseBearerToken(login.GetProperty("jwt").GetString()!);
        var path = $"/users/{login.GetProperty("id").GetString()}";

        var update = await client.PutAsJsonAsync($"{path}/details", new { firstName = "Janet", lastName = "Smith" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await (await client.GetAsync(path)).ReadJsonAsync();
        Assert.Equal("Janet", updated.GetProperty("firstName").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync(path)).StatusCode);
    }

    [Fact]
    public async Task RoleLifecycle_AssignsToSeveralUsersAndUnassigns()
    {
        var firstEmail = await RegisterAsync();
        var secondEmail = await RegisterAsync();
        var roleName = PostgresApiFixture.Unique("Role");
        var normalized = roleName.ToLowerInvariant();
        var create = await client.PostAsJsonAsync("/roles", new { name = roleName, description = "Test" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var roleId = create.CreatedId();

        // The UserRole mapping is declared one-to-one, so this proves the database still allows many users per role.
        var usersPath = $"/roles/{roleId}/users";
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync($"{usersPath}/{firstEmail}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync($"{usersPath}/{secondEmail}", null)).StatusCode);

        var users = await (await client.GetAsync($"/roles/{normalized}/users")).ReadJsonAsync();
        Assert.Equal(2, users.GetArrayLength());

        var roles = await (await client.GetAsync($"/roles/users/{firstEmail}")).ReadJsonAsync();
        Assert.Equal(normalized, roles[0].GetProperty("roleNormalizedName").GetString());

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsync($"{usersPath}/{firstEmail}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"{usersPath}/{firstEmail}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"{usersPath}/{firstEmail}")).StatusCode);
    }

    [Fact]
    public async Task Roles_CanBeListedAndUpdated()
    {
        var name = PostgresApiFixture.Unique("Role");
        var other = PostgresApiFixture.Unique("Other");
        var path = $"/roles/{(await client.PostAsJsonAsync("/roles", new { name })).CreatedId()}";
        await client.PostAsJsonAsync("/roles", new { name = other });

        var list = await (await client.GetAsync($"/roles?search={name.ToLowerInvariant()}&sortOrder=DESC"))
            .ReadJsonAsync();
        Assert.Equal(name, list[0].GetProperty("name").GetString());

        var conflict = await client.PutAsJsonAsync(path, new { name = other });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        var renamed = PostgresApiFixture.Unique("Renamed");
        var update = await client.PutAsJsonAsync(path, new { name = renamed, description = "New" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var read = await (await client.GetAsync(path)).ReadJsonAsync();
        Assert.Equal(renamed, read.GetProperty("name").GetString());
        Assert.Equal("New", read.GetProperty("description").GetString());
    }

    [Fact]
    public async Task UpdatingUnknownRole_ReturnsNotFound()
    {
        var response = await client.PutAsJsonAsync("/roles/999999", new { name = PostgresApiFixture.Unique("Role") });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void ApplyMigrations_WithNothingPending_DoesNothing()
    {
        var application = new ApplicationBuilder(api.Services);

        Assert.Same(application, application.ApplyMigrations());
    }

    [Fact]
    public async Task UserCanHoldSeveralRoles()
    {
        var email = await RegisterAsync();

        foreach (var name in new[] { PostgresApiFixture.Unique("RoleA"), PostgresApiFixture.Unique("RoleB") })
        {
            var role = await client.PostAsJsonAsync("/roles", new { name });
            var assign = await client.PostAsync($"/roles/{role.CreatedId()}/users/{email}", null);
            Assert.Equal(HttpStatusCode.Created, assign.StatusCode);
        }

        var roles = await (await client.GetAsync($"/roles/users/{email}")).ReadJsonAsync();
        Assert.Equal(2, roles.GetArrayLength());
    }

    [Fact]
    public async Task DeletingRole_RemovesItsAssignments()
    {
        var email = await RegisterAsync();
        var role = await client.PostAsJsonAsync("/roles", new { name = PostgresApiFixture.Unique("Role") });
        var roleId = role.CreatedId();
        await client.PostAsync($"/roles/{roleId}/users/{email}", null);

        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync($"/roles/{roleId}")).StatusCode);

        var roles = await (await client.GetAsync($"/roles/users/{email}")).ReadJsonAsync();
        Assert.Equal(0, roles.GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/roles/{roleId}")).StatusCode);
    }

    [Theory]
    [InlineData("POST", "/roles/999999/users/a@b.com")]
    [InlineData("DELETE", "/roles/999999/users/a@b.com")]
    [InlineData("GET", "/roles/999999")]
    [InlineData("DELETE", "/roles/999999")]
    [InlineData("GET", "/roles/users/missing@example.com")]
    [InlineData("GET", "/roles/missing-role/users")]
    public async Task UnknownRoleOrUser_ReturnsNotFound(string method, string path)
    {
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [KnownBugFact("GetRolesByUserQueryHandler dereferences a null user for an unknown id, giving a 500, not a 404")]
    public async Task GetRolesByUser_WithUnknownId_ReturnsNotFound()
    {
        var response = await client.GetAsync($"/roles/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<string> RegisterAsync()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/users/register",
            new { email, password = Password, firstName = "Jane", lastName = "Doe" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return email;
    }

    private async Task<JsonElement> LoginAsync(string email)
    {
        var response = await client.PostAsJsonAsync("/users/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }
}
