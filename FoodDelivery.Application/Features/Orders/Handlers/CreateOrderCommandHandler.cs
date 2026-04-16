using FoodDelivery.Application.Common.Abstractions;
using FoodDelivery.Domain.Entities;
using FoodDelivery.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FoodDelivery.Application.Features.Orders;

public sealed class CreateOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IUnitOfWork unitOfWork) : ICreateOrderCommandHandler
{
    public async Task<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetCurrentUserId();
        var cart = await context.Query<Domain.Entities.Cart>()
            .Include(x => x.Items).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new ValidationException("Cart is empty.");

        if (!cart.Items.Any())
        {
            throw new ValidationException("Cart is empty.");
        }

        var order = new Order
        {
            UserId = userId,
            ContactName = command.ContactName,
            ContactPhone = command.ContactPhone,
            DeliveryAddress = command.DeliveryAddress,
            DeliveryType = command.IsAsap ? DeliveryType.AsSoonAsPossible : DeliveryType.Scheduled,
            ScheduledDeliveryTimeUtc = command.IsAsap ? null : command.ScheduledDeliveryTimeUtc,
            TotalAmount = cart.Items.Sum(x => (x.Product?.Price ?? 0m) * x.Quantity),
            Items = cart.Items.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                ProductName = x.Product?.Name ?? string.Empty,
                UnitPrice = x.Product?.Price ?? 0m,
                Quantity = x.Quantity
            }).ToList()
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
