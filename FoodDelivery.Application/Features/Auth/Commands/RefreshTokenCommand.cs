namespace FoodDelivery.Application.Features.Auth;

public class RefreshTokenCommand
{
    public string RefreshToken { get; init; } = string.Empty;
    public string IpAddress { get; init; } = "unknown";
}
