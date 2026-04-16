namespace FoodDelivery.Application.Features.Orders;

public class OrderDto
{
    public Guid OrderId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryType { get; set; } = string.Empty;
    public DateTime? ScheduledDeliveryTimeUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public IReadOnlyCollection<OrderItemDto> Items { get; set; } = [];
}
