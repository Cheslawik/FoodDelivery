using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FoodDelivery.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetCurrentUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var claimValue = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);

        // Never fall back to a default account: a request without a valid user id is unauthenticated.
        return Guid.TryParse(claimValue, out var userId)
            ? userId
            : throw new UnauthorizedException("The access token does not identify a user.");
    }
}
