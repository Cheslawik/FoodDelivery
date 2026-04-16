namespace FoodDelivery.Application.Features.Cart;

public interface IGetCartQueryHandler
{
    Task<CartDto> Handle(CancellationToken cancellationToken = default);
}
