namespace FoodDelivery.Application.Features.Auth;

public interface IRevokeRefreshTokenCommandHandler
{
    Task<RevokeRefreshTokenResponseDto> Handle(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default);
}
