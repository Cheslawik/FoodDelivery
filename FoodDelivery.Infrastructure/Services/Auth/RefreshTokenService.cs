using FoodDelivery.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FoodDelivery.Infrastructure.Services;

public sealed class RefreshTokenService(IOptions<JwtOptions> options) : IRefreshTokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var tokenHash = HashToken(token);
        var expiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays);
        return (token, tokenHash, expiresAtUtc);
    }

    public string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLower(CultureInfo.InvariantCulture);
    }
}
