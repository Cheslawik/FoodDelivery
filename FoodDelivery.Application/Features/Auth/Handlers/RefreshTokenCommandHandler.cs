using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Auth;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService) : IRefreshTokenCommandHandler
{
    public async Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
        var existingToken = await context.Query<RefreshToken>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive || existingToken.User is null)
        {
            throw new ValidationException("Refresh token is invalid or expired.");
        }

        var rotatedToken = refreshTokenService.GenerateRefreshToken();
        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.ReplacedByTokenHash = rotatedToken.TokenHash;
        context.Update(existingToken);

        await context.AddAsync(new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = rotatedToken.TokenHash,
            ExpiresAtUtc = rotatedToken.ExpiresAtUtc,
            CreatedByIp = command.IpAddress
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AuthMapper.BuildAuthResponse(existingToken.User, rotatedToken.Token, jwtTokenService);
    }
}
