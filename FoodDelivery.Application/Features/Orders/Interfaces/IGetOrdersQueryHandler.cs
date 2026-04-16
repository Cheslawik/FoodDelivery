namespace FoodDelivery.Application.Features.Orders;

public interface IGetOrdersQueryHandler
{
    Task<IReadOnlyCollection<OrderDto>> Handle(CancellationToken cancellationToken = default);
}
