using System.Security.Claims;
using FoodDelivery.Application.Common.Exceptions;
using FoodDelivery.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace FoodDelivery.Tests.Infrastructure;

public class CurrentUserServiceTests
{
    [Fact]
    public void ReturnsUserId_FromNameIdentifierClaim()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        Assert.Equal(userId, service.GetCurrentUserId());
    }

    [Fact]
    public void Throws_WhenThereIsNoUserIdClaim()
    {
        var service = CreateService();

        Assert.Throws<UnauthorizedException>(() => service.GetCurrentUserId());
    }

    [Fact]
    public void Throws_WhenUserIdClaimIsNotAGuid()
    {
        var service = CreateService(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        Assert.Throws<UnauthorizedException>(() => service.GetCurrentUserId());
    }

    private static CurrentUserService CreateService(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        return new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });
    }
}
