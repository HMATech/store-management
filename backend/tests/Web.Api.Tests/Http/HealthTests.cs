using System.Net;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Http;

public class HealthTests(NoDatabaseApiFixture api) : IClassFixture<NoDatabaseApiFixture>
{
    [Fact]
    public async Task Health_WithDatabaseDown_ReturnsServiceUnavailable()
    {
        var response = await api.CreateAnonymousClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
    }
}
