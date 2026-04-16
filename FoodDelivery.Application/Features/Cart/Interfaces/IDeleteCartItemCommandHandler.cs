namespace FoodDelivery.Application.Features.Cart;

public interface IDeleteCartItemCommandHandler
{
    Task Handle(Guid cartItemId, CancellationToken cancellationToken = default);
}
