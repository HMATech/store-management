using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Http;

public class RoutingTests(NoDatabaseApiFixture api) : IClassFixture<NoDatabaseApiFixture>
{
    internal static readonly string[] ExpectedRoutes =
    [
        "GET /brands/", "POST /brands/", "GET /brands/{id:int}", "PUT /brands/{id:int}", "DELETE /brands/{id:int}",

        "GET /products/", "POST /products/", "GET /products/{id:guid}", "PUT /products/{id:guid}",
        "DELETE /products/{id:guid}", "POST /products/{id:guid}/prices",

        "GET /products/prices/", "GET /products/prices/{id:guid}", "PUT /products/prices/{id:guid}",
        "DELETE /products/prices/{id:guid}",

        "GET /products/tags/", "POST /products/tags/", "GET /products/tags/{id:int}", "PUT /products/tags/{id:int}",
        "DELETE /products/tags/{id:int}",

        "GET /products/units/", "POST /products/units/", "GET /products/units/{id:int}",
        "PUT /products/units/{id:int}", "DELETE /products/units/{id:int}",

        "GET /purchases/", "POST /purchases/", "GET /purchases/{id:guid}", "DELETE /purchases/{id:guid}",
        "PUT /purchases/{id:guid}/details", "PUT /purchases/{id:guid}/entries",

        "GET /sales/", "POST /sales/", "GET /sales/{id:guid}", "DELETE /sales/{id:guid}",
        "PUT /sales/{id:guid}/details", "PUT /sales/{id:guid}/entries",

        "GET /roles/", "POST /roles/", "GET /roles/{id:int}", "PUT /roles/{id:int}", "DELETE /roles/{id:int}",
        "GET /roles/users/{user}", "GET /roles/{role}/users", "POST /roles/{roleId}/users/{user}",
        "DELETE /roles/{roleId}/users/{user}",

        "GET /users/", "GET /users/{id}", "DELETE /users/{id}", "POST /users/login", "POST /users/register",
        "PUT /users/{id}/details",
    ];

    [Fact]
    public void AllExpectedRoutes_AreMapped()
    {
        Assert.Equal(ExpectedRoutes.Order(), MapRoutes().Keys.Order());
    }

    [Fact]
    public void OnlyLogin_AllowsAnonymousAccess()
    {
        var actual = MapRoutes()
            .Where(r => r.Value.Metadata.GetMetadata<IAllowAnonymous>() != null)
            .Select(r => r.Key);

        Assert.Equal(["POST /users/login"], actual);
    }

    [Fact]
    public async Task FallbackPolicy_RequiresAuthenticatedUser()
    {
        var policy = await api.Services.GetRequiredService<IAuthorizationPolicyProvider>().GetFallbackPolicyAsync();

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Theory]
    [InlineData("/brands/abc")]
    [InlineData("/brands/1.5")]
    [InlineData("/brands/99999999999")]
    [InlineData("/products/not-a-guid")]
    [InlineData("/products/prices/not-a-guid")]
    [InlineData("/products/tags/abc")]
    [InlineData("/products/units/abc")]
    [InlineData("/purchases/not-a-guid")]
    [InlineData("/sales/not-a-guid")]
    [InlineData("/unknown")]
    public async Task Get_WithInvalidRouteValue_ReturnsNotFound(string path)
    {
        var response = await api.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("PATCH", "/brands")]
    [InlineData("DELETE", "/brands")]
    [InlineData("PUT", "/purchases")]
    [InlineData("POST", "/brands/1")]
    public async Task UnsupportedMethod_ReturnsMethodNotAllowed(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await api.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsNotExposedOutsideDevelopment()
    {
        var response = await api.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Startup_InDevelopmentWithDatabaseDown_FailsInsteadOfServingWithoutMigrations()
    {
        using var host = new ApiHost("Development", "Host=localhost;Port=1;Timeout=1;Pooling=false");

        Assert.ThrowsAny<Exception>(() => host.Services);
    }

    private Dictionary<string, RouteEndpoint> MapRoutes()
    {
        return api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<HttpMethodMetadata>() != null)
            .ToDictionary(e => $"{e.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Single()} " +
                $"{e.RoutePattern.RawText}");
    }
}
