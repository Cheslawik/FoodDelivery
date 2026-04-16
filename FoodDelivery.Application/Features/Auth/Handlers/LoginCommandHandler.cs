using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Auth;

public sealed class LoginCommandHandler(
    IApplicationDbContext context,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    IUnitOfWork unitOfWork) : ILoginCommandHandler
{
    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var user = await context.Query<User>().FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (user is null || !passwordService.VerifyPassword(user.PasswordHash, command.Password))
        {
            throw new ValidationException("Invalid email or password.");
        }

        var refreshTokenData = refreshTokenService.GenerateRefreshToken();
        await context.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenData.TokenHash,
            ExpiresAtUtc = refreshTokenData.ExpiresAtUtc,
            CreatedByIp = command.IpAddress
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthMapper.BuildAuthResponse(user, refreshTokenData.Token, jwtTokenService);
    }
}
