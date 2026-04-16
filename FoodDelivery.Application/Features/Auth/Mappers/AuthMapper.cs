using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;

namespace FoodDelivery.Application.Features.Auth;

internal static class AuthMapper
{
    public static AuthResponseDto BuildAuthResponse(User user, string refreshToken, IJwtTokenService jwtTokenService)
        => new()
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Address = user.Address,
            Role = user.Role.ToString(),
            AccessToken = jwtTokenService.GenerateToken(user.Id, user.Email, user.FullName, user.Role.ToString()),
            RefreshToken = refreshToken
        };
}
