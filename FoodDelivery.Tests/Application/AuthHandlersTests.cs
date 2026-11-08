using FoodDelivery.Application.Common.Exceptions;
using FoodDelivery.Application.Features.Auth;
using FoodDelivery.Infrastructure.Persistence;
using FoodDelivery.Infrastructure.Services;
using FoodDelivery.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FoodDelivery.Tests.Application;

public sealed class AuthHandlersTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();
    private readonly PasswordService _passwords = new();
    private readonly JwtTokenService _jwt;
    private readonly RefreshTokenService _refreshTokens;

    public AuthHandlersTests()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "tests",
            Audience = "tests",
            Key = "test-signing-key-that-is-long-enough-for-hmac-sha256",
            AccessTokenLifetimeMinutes = 5,
            RefreshTokenLifetimeDays = 1
        });
        _jwt = new JwtTokenService(options);
        _refreshTokens = new RefreshTokenService(options);
    }

    [Fact]
    public async Task Register_NormalizesEmail_AndReturnsTokens()
    {
        var response = await RegisterAsync("  New.User@Test.Local ", "+300");

        Assert.Equal("new.user@test.local", response.Email);
        Assert.Equal("Customer", response.Role);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        await RegisterAsync("user@test.local", "+300");

        await Assert.ThrowsAsync<ConflictException>(() => RegisterAsync("USER@test.local", "+301"));
    }

    [Fact]
    public async Task Register_DuplicatePhone_ThrowsConflict()
    {
        await RegisterAsync("first@test.local", "+300");

        await Assert.ThrowsAsync<ConflictException>(() => RegisterAsync("second@test.local", "+300"));
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await RegisterAsync("user@test.local", "+300");

        await using var context = _db.CreateContext();
        var handler = new LoginCommandHandler(context, _passwords, _jwt, _refreshTokens, context);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand { Email = "user@test.local", Password = "wrong-password" }));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorized()
    {
        await using var context = _db.CreateContext();
        var handler = new LoginCommandHandler(context, _passwords, _jwt, _refreshTokens, context);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand { Email = "nobody@test.local", Password = "Password123!" }));
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndOldTokenCannotBeReused()
    {
        var registered = await RegisterAsync("user@test.local", "+300");

        var rotated = await RefreshAsync(registered.RefreshToken);
        Assert.NotEqual(registered.RefreshToken, rotated.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(registered.RefreshToken));

        await using var context = _db.CreateContext();
        var oldToken = await context.RefreshTokens.SingleAsync(x => x.TokenHash == _refreshTokens.HashToken(registered.RefreshToken));
        Assert.True(oldToken.IsRevoked);
        Assert.Equal(_refreshTokens.HashToken(rotated.RefreshToken), oldToken.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Revoke_ThenRefresh_ThrowsUnauthorized()
    {
        var registered = await RegisterAsync("user@test.local", "+300");

        await using (var context = _db.CreateContext())
        {
            var result = await new RevokeRefreshTokenCommandHandler(context, context, _refreshTokens)
                .Handle(new RevokeRefreshTokenCommand { RefreshToken = registered.RefreshToken });
            Assert.True(result.Revoked);
        }

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(registered.RefreshToken));
    }

    private async Task<AuthResponseDto> RegisterAsync(string email, string phone)
    {
        await using var context = _db.CreateContext();
        var handler = new RegisterUserCommandHandler(context, context, _passwords, _jwt, _refreshTokens);
        return await handler.Handle(new RegisterUserCommand
        {
            Email = email,
            FullName = "Test User",
            Phone = phone,
            Address = "1 Test Street",
            Password = "Password123!"
        });
    }

    private async Task<AuthResponseDto> RefreshAsync(string refreshToken)
    {
        await using FoodDeliveryDbContext context = _db.CreateContext();
        var handler = new RefreshTokenCommandHandler(context, context, _jwt, _refreshTokens);
        return await handler.Handle(new RefreshTokenCommand { RefreshToken = refreshToken });
    }

    public void Dispose() => _db.Dispose();
}
