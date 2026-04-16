namespace FoodDelivery.Application.Features.Auth;

public class RevokeRefreshTokenCommand
{
    public string RefreshToken { get; init; } = string.Empty;
}
