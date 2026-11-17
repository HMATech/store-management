using System.Net;

namespace Web.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public class HealthIntegrationTests(PostgresApiFixture api)
{
    [Fact]
    public async Task Health_WithDatabaseUp_IsHealthyWithoutSigningIn()
    {
        var response = await api.CreateAnonymousClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
