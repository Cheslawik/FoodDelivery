namespace FoodDelivery.Application.Features.Orders;

public interface ICreateOrderCommandHandler
{
    Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default);
}
