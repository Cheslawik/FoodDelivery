namespace FoodDelivery.Application.Features.Auth;

public interface IRegisterUserCommandHandler
{
    Task<AuthResponseDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken = default);
}
