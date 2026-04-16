namespace FoodDelivery.Application.Features.Auth;

public interface ILoginCommandHandler
{
    Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken = default);
}
