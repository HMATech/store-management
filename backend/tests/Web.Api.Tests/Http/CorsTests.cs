using System.Net;
using Microsoft.Net.Http.Headers;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Http;

public class CorsTests(CorsApiFixture api, NoDatabaseApiFixture noCorsApi)
    : IClassFixture<CorsApiFixture>, IClassFixture<NoDatabaseApiFixture>
{
    [Fact]
    public async Task Preflight_FromAllowedOrigin_AllowsMethodAndAuthorizationHeader()
    {
        var response = await api.CreateAnonymousClient().SendAsync(CreatePreflight(CorsApiFixture.AllowedOrigin));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(CorsApiFixture.AllowedOrigin, Header(response, HeaderNames.AccessControlAllowOrigin));
        Assert.Contains("PUT", Header(response, HeaderNames.AccessControlAllowMethods));
        Assert.Contains("authorization", Header(response, HeaderNames.AccessControlAllowHeaders),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_FromOriginConfiguredWithTrailingSlash_IsAllowed()
    {
        var response = await api.CreateAnonymousClient().SendAsync(CreatePreflight(CorsApiFixture.SlashedOrigin));

        Assert.Equal(CorsApiFixture.SlashedOrigin, Header(response, HeaderNames.AccessControlAllowOrigin));
    }

    [Fact]
    public async Task Preflight_FromOtherOrigin_IsNotAllowed()
    {
        var response = await api.CreateAnonymousClient().SendAsync(CreatePreflight("https://evil.example.com"));

        Assert.False(response.Headers.Contains(HeaderNames.AccessControlAllowOrigin));
    }

    [Fact]
    public async Task Request_WithoutToken_FromAllowedOrigin_ReturnsUnauthorizedTheBrowserCanRead()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/brands");
        request.Headers.Add(HeaderNames.Origin, CorsApiFixture.AllowedOrigin);

        var response = await api.CreateAnonymousClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(CorsApiFixture.AllowedOrigin, Header(response, HeaderNames.AccessControlAllowOrigin));
    }

    [Fact]
    public async Task Request_FromAllowedOrigin_ExposesLocationHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/brands");
        request.Headers.Add(HeaderNames.Origin, CorsApiFixture.AllowedOrigin);

        var response = await api.CreateClient().SendAsync(request);

        Assert.Contains(HeaderNames.Location, Header(response, HeaderNames.AccessControlExposeHeaders));
    }

    [Fact]
    public async Task Preflight_WithNoOriginsConfigured_IsNotAllowed()
    {
        var response = await noCorsApi.CreateAnonymousClient().SendAsync(CreatePreflight(CorsApiFixture.AllowedOrigin));

        Assert.False(response.Headers.Contains(HeaderNames.AccessControlAllowOrigin));
    }

    private static HttpRequestMessage CreatePreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/brands/1");
        request.Headers.Add(HeaderNames.Origin, origin);
        request.Headers.Add(HeaderNames.AccessControlRequestMethod, "PUT");
        request.Headers.Add(HeaderNames.AccessControlRequestHeaders, "authorization,content-type");
        return request;
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : string.Empty;
    }
}

/// <summary>Hosts the API without a database and with two allowed CORS origins, one configured with a slash.</summary>
public sealed class CorsApiFixture : IDisposable
{
    public const string AllowedOrigin = "https://app.example.com";
    public const string SlashedOrigin = "https://other.example.com";

    private readonly ApiHost host = new("Testing", "Host=localhost;Port=1;Timeout=1;Pooling=false",
        new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = AllowedOrigin,
            ["Cors:AllowedOrigins:1"] = $"{SlashedOrigin}/",
        });

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
