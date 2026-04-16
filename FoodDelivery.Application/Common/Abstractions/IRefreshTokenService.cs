namespace FoodDelivery.Application.Common.Abstractions;

public interface IRefreshTokenService
{
    (string Token, string TokenHash, DateTime ExpiresAtUtc) GenerateRefreshToken();
    string HashToken(string token);
}
