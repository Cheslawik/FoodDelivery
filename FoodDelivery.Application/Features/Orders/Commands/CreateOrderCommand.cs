namespace FoodDelivery.Application.Features.Orders;

public class CreateOrderCommand
{
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
    public bool IsAsap { get; init; }
    public DateTime? ScheduledDeliveryTimeUtc { get; init; }
}
