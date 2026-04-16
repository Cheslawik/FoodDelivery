namespace FoodDelivery.Application.Features.Cart;

public interface IUpdateCartItemCommandHandler
{
    Task Handle(UpdateCartItemCommand command, CancellationToken cancellationToken = default);
}
