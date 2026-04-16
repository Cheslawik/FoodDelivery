namespace FoodDelivery.Application.Features.Auth;

public class LoginCommand
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string IpAddress { get; init; } = "unknown";
}
