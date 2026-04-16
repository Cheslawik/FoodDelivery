using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Auth;

public sealed class RevokeRefreshTokenCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IRefreshTokenService refreshTokenService) : IRevokeRefreshTokenCommandHandler
{
    public async Task<RevokeRefreshTokenResponseDto> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = refreshTokenService.HashToken(command.RefreshToken);
        var existingToken = await context.Query<RefreshToken>().FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || existingToken.IsRevoked)
        {
            return new RevokeRefreshTokenResponseDto { Revoked = false };
        }

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        context.Update(existingToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RevokeRefreshTokenResponseDto { Revoked = true };
    }
}
