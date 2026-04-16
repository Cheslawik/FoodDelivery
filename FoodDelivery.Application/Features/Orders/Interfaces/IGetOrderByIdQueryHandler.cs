namespace FoodDelivery.Application.Features.Orders;

public interface IGetOrderByIdQueryHandler
{
    Task<OrderDto> Handle(Guid orderId, CancellationToken cancellationToken = default);
}
