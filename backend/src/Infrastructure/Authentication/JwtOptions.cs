namespace Infrastructure.Authentication;

public record JwtOptions(string ValidIssuer, string[] ValidAudiences,
    string Secret, int ExpirationInMinutes);