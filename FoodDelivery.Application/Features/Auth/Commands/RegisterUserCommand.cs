namespace FoodDelivery.Application.Features.Auth;

public class RegisterUserCommand
{
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string IpAddress { get; init; } = "unknown";
}
