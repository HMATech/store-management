using System.Security.Claims;
using System.Text;
using Application.Abstractions.Authentication;
using Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

internal class TokenProvider(IConfiguration configuration) : ITokenProvider
{
    public string CreateFor(User user)
    {
        var jwt = configuration.GetSection(JwtDefaults.SectionName).Get<JwtOptions>()!;
        var secret = jwt.Secret;

        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
                ]
            ),
            Expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationInMinutes),
            SigningCredentials = credentials,
            Issuer = jwt.ValidIssuer,
            Audience = jwt.ValidAudiences[0]
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }
}