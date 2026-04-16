namespace FoodDelivery.Application.Common.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(Guid userId, string email, string fullName, string role);
}
