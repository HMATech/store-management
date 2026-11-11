namespace Infrastructure.Authentication;

public static class JwtDefaults
{
    public const string SectionName = "Jwt";
    public const string SecretSection = "Secret";
    public const string ValidIssuerSection = "ValidIssuer";
    public const string ValidAudiencesSection = "ValidAudiences";
    public const string ExpirationInMinutesSection = "ExpirationInMinutes";
}