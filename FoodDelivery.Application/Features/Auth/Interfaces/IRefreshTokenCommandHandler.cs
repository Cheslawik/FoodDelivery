namespace FoodDelivery.Application.Features.Auth;

public interface IRefreshTokenCommandHandler
{
    Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken = default);
}
