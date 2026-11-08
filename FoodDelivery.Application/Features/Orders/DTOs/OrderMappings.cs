using System.Linq.Expressions;
using FoodDelivery.Domain.Entities;

namespace FoodDelivery.Application.Features.Orders;

public static class OrderMappings
{
    /// <summary>Projection translated to SQL by EF Core, including the order lines.</summary>
    public static readonly Expression<Func<Order, OrderDto>> ToDto = order => new OrderDto
    {
        OrderId = order.Id,
        ContactName = order.ContactName,
        ContactPhone = order.ContactPhone,
        DeliveryAddress = order.DeliveryAddress,
        DeliveryType = order.DeliveryType.ToString(),
        ScheduledDeliveryTimeUtc = order.ScheduledDeliveryTimeUtc,
        TotalAmount = order.TotalAmount,
        Items = order.Items
            .Select(item => new OrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity
            })
            .ToList()
    };
}
