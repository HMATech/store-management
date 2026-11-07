using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Http;

public partial class AuthenticationTests(NoDatabaseApiFixture api) : IClassFixture<NoDatabaseApiFixture>
{
    private const string LoginRoute = "POST /users/login";
    private const string SomeId = "11111111-1111-1111-1111-111111111111";

    public static TheoryData<string, string> ProtectedEndpoints => CreateProtectedEndpoints();

    public static TheoryData<string, string> InvalidTokens => new()
    {
        { "garbage", "not-a-jwt" },
        { "wrong secret", TestTokens.Create(secret: "another-secret-another-secret-another-secret-123") },
        { "wrong issuer", TestTokens.Create(issuer: "another-issuer") },
        { "wrong audience", TestTokens.Create(audience: "another-audience") },
        { "expired", TestTokens.Create(expires: DateTime.UtcNow.AddMinutes(-5)) },
        { "tampered", TamperPayload(TestTokens.Create()) },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized(string method, string path)
    {
        var response = await SendAsync(method, path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/users")]
    [InlineData("POST", "/users/register")]
    [InlineData("POST", "/brands")]
    public async Task ProtectedEndpoint_WithBasicScheme_ReturnsUnauthorized(string method, string path)
    {
        var response = await SendAsync(method, path, new AuthenticationHeaderValue("Basic", "dXNlcjpwYXNz"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task ProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized(string reason, string token)
    {
        var response = await SendAsync("GET", "/users", new AuthenticationHeaderValue("Bearer", token));

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"Expected a 401 for a token with: {reason}");
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_PassesAuthentication()
    {
        var response = await SendAsync("GET", "/users", new AuthenticationHeaderValue("Bearer", TestTokens.Create()));

        // The database is unreachable here, so getting past authentication ends in a 500.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutToken_IsAllowed()
    {
        var response = await SendAsync("POST", "/users/login", body: """{"email":"not-an-email","password":"x"}""");

        // A validation error shows the request got past authorization to the handler.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_WithoutToken_ReturnsUnauthorized()
    {
        var response = await SendAsync("GET", "/unknown");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(string method, string path,
        AuthenticationHeaderValue? authorization = null, string body = "{}")
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Authorization = authorization;
        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return await api.CreateAnonymousClient().SendAsync(request);
    }

    private static TheoryData<string, string> CreateProtectedEndpoints()
    {
        var data = new TheoryData<string, string>();
        foreach (var route in RoutingTests.ExpectedRoutes.Where(r => r != LoginRoute))
        {
            var (method, pattern) = SplitRoute(route);
            data.Add(method, ToSamplePath(pattern));
        }

        return data;
    }

    private static (string Method, string Pattern) SplitRoute(string route)
    {
        var parts = route.Split(' ', 2);
        return (parts[0], parts[1]);
    }

    private static string ToSamplePath(string pattern)
    {
        return RouteParameterRegex().Replace(pattern, match => match.Groups["name"].Value switch
        {
            "user" => "jane@example.com",
            "role" => "admin",
            "roleId" => "1",
            _ => match.Groups["constraint"].Value == "int" ? "1" : SomeId,
        });
    }

    private static string TamperPayload(string token)
    {
        var parts = token.Split('.');
        var payload = (parts[1][0] == 'A' ? "B" : "A") + parts[1][1..];
        return $"{parts[0]}.{payload}.{parts[2]}";
    }

    [GeneratedRegex(@"\{(?<name>\w+)(:(?<constraint>\w+))?\}")]
    private static partial Regex RouteParameterRegex();
}
