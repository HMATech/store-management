using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Web.Api.Tests.TestSupport;

/// <summary>Creates JWTs that the API host accepts by default, or rejects when a value is overridden.</summary>
internal static class TestTokens
{
    public static string Create(string secret = ApiHost.JwtSecret, string issuer = ApiHost.JwtIssuer,
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
}
