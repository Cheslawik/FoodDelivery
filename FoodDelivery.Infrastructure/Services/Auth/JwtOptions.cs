namespace FoodDelivery.Infrastructure.Services;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinKeyBytes = 32;

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; init; } = 60;
    public int RefreshTokenLifetimeDays { get; init; } = 14;
}
