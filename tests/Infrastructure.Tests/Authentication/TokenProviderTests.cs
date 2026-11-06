using System.Text;
using Domain.Users;
using Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Tests.Authentication;

public class TokenProviderTests
{
    private const string Secret = "unit-test-secret-unit-test-secret-unit-test-secret";
    private const string Issuer = "test-issuer";

    private static readonly User SomeUser = User.CreateNew("jane@example.com", "hash", "Jane", null, DateTime.UtcNow);

    [Fact]
    public void CreateFor_IncludesUserClaims()
    {
        var token = new JsonWebToken(CreateProvider().CreateFor(SomeUser));

        Assert.Equal(SomeUser.Id.ToString(), token.Subject);
        Assert.Equal(SomeUser.Email, token.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(Issuer, token.Issuer);
    }

    [Fact]
    public void CreateFor_ExpiresAfterConfiguredMinutes()
    {
        var before = DateTime.UtcNow;

        var token = new JsonWebToken(CreateProvider(expirationMinutes: 30).CreateFor(SomeUser));

        Assert.InRange(token.ValidTo, before.AddMinutes(29), before.AddMinutes(31));
    }

    [Fact]
    public async Task CreateFor_ProducesTokenThatPassesValidation()
    {
        var result = await ValidateAsync(CreateProvider().CreateFor(SomeUser), Secret, Issuer);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task Token_FailsValidation_WhenSignedWithAnotherSecret()
    {
        var result = await ValidateAsync(CreateProvider().CreateFor(SomeUser),
            "another-secret-another-secret-another-secret-12345", Issuer);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Token_FailsValidation_WithWrongIssuer()
    {
        var result = await ValidateAsync(CreateProvider().CreateFor(SomeUser), Secret, "another-issuer");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Token_FailsValidation_WhenPayloadIsTampered()
    {
        var parts = CreateProvider().CreateFor(SomeUser).Split('.');
        var payload = (parts[1][0] == 'A' ? "B" : "A") + parts[1][1..];

        var result = await ValidateAsync($"{parts[0]}.{payload}.{parts[2]}", Secret, Issuer);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateFor_WithoutJwtConfiguration_Throws()
    {
        var provider = new TokenProvider(new ConfigurationBuilder().Build());

        Assert.Throws<NullReferenceException>(() => provider.CreateFor(SomeUser));
    }

    [Fact]
    public void CreateFor_WithTooShortSecret_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateProvider(secret: "short").CreateFor(SomeUser));
    }

    [Fact]
    public void CreateFor_UsesConfiguredAudience()
    {
        var token = new JsonWebToken(CreateProvider(audience: "store-app").CreateFor(SomeUser));

        Assert.Contains("store-app", token.Audiences);
    }

    private static TokenProvider CreateProvider(string secret = Secret, string audience = "developer",
        int expirationMinutes = 60)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ValidIssuer"] = Issuer,
                ["Jwt:ValidAudiences:0"] = audience,
                ["Jwt:Secret"] = secret,
                ["Jwt:ExpirationInMinutes"] = expirationMinutes.ToString(),
            })
            .Build();

        return new TokenProvider(configuration);
    }

    private static Task<TokenValidationResult> ValidateAsync(string token, string secret, string issuer)
    {
        return new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidIssuer = issuer,
            ValidAudience = "developer",
        });
    }
}
