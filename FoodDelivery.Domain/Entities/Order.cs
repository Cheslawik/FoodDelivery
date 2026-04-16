using FoodDelivery.Domain.Common;
using FoodDelivery.Domain.Enums;

namespace FoodDelivery.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DeliveryType DeliveryType { get; set; }
    public DateTime? ScheduledDeliveryTimeUtc { get; set; }
    public decimal TotalAmount { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
