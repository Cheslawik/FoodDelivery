namespace FoodDelivery.Application.Features.Cart;

public interface IAddCartItemCommandHandler
{
    Task Handle(AddCartItemCommand command, CancellationToken cancellationToken = default);
}
