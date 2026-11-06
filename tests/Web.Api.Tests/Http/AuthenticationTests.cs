using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Web.Api.Tests.TestSupport;

namespace Web.Api.Tests.Http;

public class AuthenticationTests(NoDatabaseApiFixture api) : IClassFixture<NoDatabaseApiFixture>
{
    private const string SomeUserPath = "/users/11111111-1111-1111-1111-111111111111";

    public static TheoryData<string, string> ProtectedEndpoints => new()
    {
        { "GET", "/users" },
        { "GET", SomeUserPath },
        { "DELETE", SomeUserPath },
        { "PUT", $"{SomeUserPath}/details" },
    };

    public static TheoryData<string, string> InvalidTokens => new()
    {
        { "garbage", "not-a-jwt" },
        { "wrong secret", CreateToken(secret: "another-secret-another-secret-another-secret-123") },
        { "wrong issuer", CreateToken(issuer: "another-issuer") },
        { "wrong audience", CreateToken(audience: "another-audience") },
        { "expired", CreateToken(expires: DateTime.UtcNow.AddMinutes(-5)) },
        { "tampered", TamperPayload(CreateToken()) },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized(string method, string path)
    {
        var response = await SendAsync(method, path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
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
        var response = await SendAsync("GET", "/users", new AuthenticationHeaderValue("Bearer", CreateToken()));

        // The database is unreachable here, so getting past authentication ends in a 500.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private static string CreateToken(string secret = ApiHost.JwtSecret, string issuer = ApiHost.JwtIssuer,
        string audience = ApiHost.JwtAudience, DateTime? expires = null)
    {
        var expiry = expires ?? DateTime.UtcNow.AddMinutes(30);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())]),
            NotBefore = expiry.AddHours(-1),
            Expires = expiry,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
        });
    }

    private static string TamperPayload(string token)
    {
        var parts = token.Split('.');
        var payload = (parts[1][0] == 'A' ? "B" : "A") + parts[1][1..];
        return $"{parts[0]}.{payload}.{parts[2]}";
    }

    private async Task<HttpResponseMessage> SendAsync(string method, string path,
        AuthenticationHeaderValue? authorization = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Authorization = authorization;
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new { firstName = "Jane" });
        }

        return await api.CreateClient().SendAsync(request);
    }
}
