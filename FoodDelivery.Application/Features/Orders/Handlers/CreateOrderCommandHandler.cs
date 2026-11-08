using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Application.Common.Exceptions;
using FoodDelivery.Domain.Entities;
using FoodDelivery.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Orders;

public sealed class CreateOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IUnitOfWork unitOfWork) : ICreateOrderCommandHandler
{
    public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetCurrentUserId();
        var cart = await context.Query<Domain.Entities.Cart>()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new BusinessRuleException("Cart is empty.");
        }

        // A product can be switched off after it was added to the cart; it must not be ordered.
        var unavailable = cart.Items
            .Where(x => x.Product is null || !x.Product.IsAvailable)
            .Select(x => x.Product?.Name ?? x.ProductId.ToString())
            .ToList();
        if (unavailable.Count > 0)
        {
            throw new BusinessRuleException($"Some products are no longer available: {string.Join(", ", unavailable)}. Remove them from the cart and try again.");
        }

        // Items keep a snapshot of name and price, so later menu changes do not alter past orders.
        var items = cart.Items.Select(x => new OrderItem
        {
            ProductId = x.ProductId,
            ProductName = x.Product!.Name,
            UnitPrice = x.Product.Price,
            Quantity = x.Quantity
        }).ToList();

        var order = new Order
        {
            UserId = userId,
            ContactName = command.ContactName,
            ContactPhone = command.ContactPhone,
            DeliveryAddress = command.DeliveryAddress,
            DeliveryType = command.IsAsap ? DeliveryType.AsSoonAsPossible : DeliveryType.Scheduled,
            ScheduledDeliveryTimeUtc = command.IsAsap ? null : command.ScheduledDeliveryTimeUtc,
            TotalAmount = items.Sum(x => x.UnitPrice * x.Quantity),
            Items = items
        };

        await context.AddAsync(order, cancellationToken);
        foreach (var item in cart.Items.ToList())
        {
            context.Remove(item);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}
